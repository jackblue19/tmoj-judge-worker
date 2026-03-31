using System.Diagnostics;
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
        var args = BuildDockerArgs(request);

        var psi = new ProcessStartInfo
        {
            FileName = "docker" ,
            Arguments = args ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        _logger.LogInformation("Running docker command: docker {Args}" , args);

        var stopwatch = Stopwatch.StartNew();
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(request.TimeoutMs));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            stopwatch.Stop();

            return new DockerRunResult
            {
                ExitCode = -1 ,
                TimedOut = true ,
                ElapsedMs = (int) stopwatch.ElapsedMilliseconds ,
                Stdout = "" ,
                Stderr = "Docker execution timed out."
            };
        }

        stopwatch.Stop();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new DockerRunResult
        {
            ExitCode = process.ExitCode ,
            TimedOut = false ,
            ElapsedMs = (int) stopwatch.ElapsedMilliseconds ,
            Stdout = stdout ?? "" ,
            Stderr = stderr ?? ""
        };
    }

    private static string BuildDockerArgs(DockerRunRequest request)
    {
        var parts = new List<string>
        {
            "run",
            "--rm",
            $"--name {request.ContainerName}"
        };

        if ( request.NetworkDisabled )
            parts.Add("--network none");

        if ( !string.IsNullOrWhiteSpace(request.MemoryLimit) )
            parts.Add($"--memory {request.MemoryLimit}");

        if ( !string.IsNullOrWhiteSpace(request.CpuLimit) )
            parts.Add($"--cpus {request.CpuLimit}");

        foreach ( var mount in request.Mounts )
        {
            parts.Add($"-v \"{mount.HostPath}\":\"{mount.ContainerPath}\"");
        }

        foreach ( var env in request.EnvironmentVariables )
        {
            parts.Add($"-e {env.Key}=\"{env.Value}\"");
        }

        if ( !string.IsNullOrWhiteSpace(request.WorkingDirectory) )
            parts.Add($"-w \"{request.WorkingDirectory}\"");

        parts.Add($"\"{request.Image}\"");

        if ( !string.IsNullOrWhiteSpace(request.Command) )
            parts.Add(request.Command);

        return string.Join(" " , parts);
    }
}

public sealed class DockerRunRequest
{
    public string ContainerName { get; init; } = $"tmoj-{Guid.NewGuid():N}";
    public string Image { get; init; } = null!;
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
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public int ElapsedMs { get; init; }
    public string Stdout { get; init; } = "";
    public string Stderr { get; init; } = "";
}