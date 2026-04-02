using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Worker.Execution.Containers;

public sealed class DockerSandboxRunner
{
    private readonly ILogger<DockerSandboxRunner> _logger;

    public DockerSandboxRunner(ILogger<DockerSandboxRunner> logger)
    {
        _logger = logger;
    }

    public async Task<DockerRunResult> RunAsync(
        DockerRunRequest request ,
        CancellationToken ct)
    {
        var containerName = string.IsNullOrWhiteSpace(request.ContainerName)
            ? $"tmoj-{Guid.NewGuid():N}"
            : request.ContainerName;

        var createArgs = BuildCreateArgs(request , containerName);

        var create = await RunProcessAsync("docker" , createArgs , ct);
        if ( create.ExitCode != 0 )
        {
            return new DockerRunResult
            {
                ExitCode = create.ExitCode ,
                TimedOut = false ,
                OomKilled = false ,
                ElapsedMs = 0 ,
                Stdout = create.Stdout ,
                Stderr = create.Stderr ,
                FailureReason = "docker create failed"
            };
        }

        var containerId = ExtractContainerId(create.Stdout) ?? containerName;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var start = await RunProcessAsync("docker" , $"start {containerId}" , ct);
            if ( start.ExitCode != 0 )
            {
                return new DockerRunResult
                {
                    ContainerId = containerId ,
                    ExitCode = start.ExitCode ,
                    TimedOut = false ,
                    OomKilled = false ,
                    ElapsedMs = 0 ,
                    Stdout = start.Stdout ,
                    Stderr = start.Stderr ,
                    FailureReason = "docker start failed"
                };
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(request.TimeoutMs));

            ProcessRunResult waitResult;
            try
            {
                waitResult = await RunProcessAsync("docker" , $"wait {containerId}" , timeoutCts.Token);
            }
            catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
            {
                try
                {
                    await RunProcessAsync("docker" , $"rm -f {containerId}" , CancellationToken.None);
                }
                catch
                {
                    // ignore cleanup failure
                }

                stopwatch.Stop();

                return new DockerRunResult
                {
                    ContainerId = containerId ,
                    ExitCode = -1 ,
                    TimedOut = true ,
                    OomKilled = false ,
                    ElapsedMs = (int) stopwatch.ElapsedMilliseconds ,
                    Stdout = "" ,
                    Stderr = "Docker execution timed out." ,
                    FailureReason = "timeout"
                };
            }

            stopwatch.Stop();

            var inspect = await RunProcessAsync(
                "docker" ,
                $"inspect {containerId} --format \"{{{{.State.ExitCode}}}}|{{{{.State.OOMKilled}}}}|{{{{.State.Error}}}}\"" ,
                CancellationToken.None);

            var logs = await RunProcessAsync("docker" , $"logs {containerId}" , CancellationToken.None);

            var parsed = ParseInspect(inspect.Stdout);

            try
            {
                await RunProcessAsync("docker" , $"rm -f {containerId}" , CancellationToken.None);
            }
            catch
            {
                // ignore cleanup failure
            }

            return new DockerRunResult
            {
                ContainerId = containerId ,
                ExitCode = parsed.ExitCode ,
                TimedOut = false ,
                OomKilled = parsed.OomKilled ,
                ElapsedMs = (int) stopwatch.ElapsedMilliseconds ,
                Stdout = logs.Stdout ?? "" ,
                Stderr = string.IsNullOrWhiteSpace(parsed.StateError)
                    ? logs.Stderr ?? ""
                    : $"{logs.Stderr}{Environment.NewLine}{parsed.StateError}".Trim() ,
                PeakMemoryKb = null ,
                FailureReason = parsed.OomKilled ? "oom_killed" : null
            };
        }
        finally
        {
            try
            {
                await RunProcessAsync("docker" , $"rm -f {containerId}" , CancellationToken.None);
            }
            catch
            {
                // ignore cleanup failure
            }
        }
    }

    private static string BuildCreateArgs(DockerRunRequest request , string containerName)
    {
        var parts = new List<string>
        {
            "create",
            $"--name {containerName}"
        };

        if ( request.NetworkDisabled )
            parts.Add("--network none");

        if ( !string.IsNullOrWhiteSpace(request.MemoryLimit) )
        {
            parts.Add($"--memory {request.MemoryLimit}");
            parts.Add($"--memory-swap {request.MemoryLimit}");
        }

        if ( !string.IsNullOrWhiteSpace(request.CpuLimit) )
            parts.Add($"--cpus {request.CpuLimit}");

        parts.Add("--pids-limit 256");

        foreach ( var mount in request.Mounts )
        {
            parts.Add($"-v \"{mount.HostPath}\":\"{mount.ContainerPath}\"");
        }

        foreach ( var env in request.EnvironmentVariables )
        {
            parts.Add($"-e {env.Key}=\"{EscapeForDoubleQuotes(env.Value)}\"");
        }

        if ( !string.IsNullOrWhiteSpace(request.WorkingDirectory) )
            parts.Add($"-w \"{request.WorkingDirectory}\"");

        if ( !string.IsNullOrWhiteSpace(request.Entrypoint) )
            parts.Add($"--entrypoint \"{request.Entrypoint}\"");

        parts.Add($"\"{request.Image}\"");

        if ( !string.IsNullOrWhiteSpace(request.Command) )
            parts.Add(request.Command);

        return string.Join(" " , parts);
    }

    private static string EscapeForDoubleQuotes(string value)
        => value.Replace("\"" , "\\\"");

    private static string? ExtractContainerId(string stdout)
    {
        if ( string.IsNullOrWhiteSpace(stdout) )
            return null;

        return stdout.Trim().Split('\n' , StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim();
    }

    private static (int ExitCode, bool OomKilled, string? StateError) ParseInspect(string stdout)
    {
        if ( string.IsNullOrWhiteSpace(stdout) )
            return (0, false, null);

        var raw = stdout.Trim().Trim('"');
        var parts = raw.Split('|' , 3);

        int exitCode = 0;
        bool oomKilled = false;
        string? stateError = null;

        if ( parts.Length >= 1 )
            int.TryParse(parts[0] , out exitCode);

        if ( parts.Length >= 2 )
            bool.TryParse(parts[1] , out oomKilled);

        if ( parts.Length >= 3 )
            stateError = string.IsNullOrWhiteSpace(parts[2]) ? null : parts[2];

        return (exitCode, oomKilled, stateError);
    }

    private async Task<ProcessRunResult> RunProcessAsync(
        string fileName ,
        string arguments ,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName ,
            Arguments = arguments ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        _logger.LogInformation("Running process: {FileName} {Arguments}" , fileName , arguments);

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        _logger.LogInformation(
            "Process finished. FileName={FileName}, ExitCode={ExitCode}, Stdout={Stdout}, Stderr={Stderr}" ,
            fileName ,
            process.ExitCode ,
            stdout ,
            stderr);

        return new ProcessRunResult
        {
            ExitCode = process.ExitCode ,
            Stdout = stdout ?? "" ,
            Stderr = stderr ?? ""
        };
    }

    private sealed class ProcessRunResult
    {
        public int ExitCode { get; init; }
        public string Stdout { get; init; } = "";
        public string Stderr { get; init; } = "";
    }
}

public sealed class DockerRunRequest
{
    public string ContainerName { get; init; } = $"tmoj-{Guid.NewGuid():N}";
    public string Image { get; init; } = null!;
    public string? Entrypoint { get; init; }
    public string? WorkingDirectory { get; init; }
    public string? Command { get; init; }
    public int TimeoutMs { get; init; } = 1000;
    public bool NetworkDisabled { get; init; } = true;
    public string? MemoryLimit { get; init; }
    public string? CpuLimit { get; init; }
    public List<DockerMount> Mounts { get; init; } = new();
    public Dictionary<string , string> EnvironmentVariables { get; init; } = new();
}

public sealed class DockerMount
{
    public string HostPath { get; init; } = null!;
    public string ContainerPath { get; init; } = null!;
}

public sealed class DockerRunResult
{
    public string? ContainerId { get; init; }
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public bool OomKilled { get; init; }
    public int ElapsedMs { get; init; }
    public int? PeakMemoryKb { get; init; }
    public string Stdout { get; init; } = "";
    public string Stderr { get; init; } = "";
    public string? FailureReason { get; init; }
}