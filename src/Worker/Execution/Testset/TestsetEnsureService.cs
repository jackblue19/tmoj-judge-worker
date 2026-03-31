using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Worker.Execution.Testset;

public sealed class TestsetEnsureService
{
    private readonly ILogger<TestsetEnsureService> _logger;
    private readonly string _scriptPath;

    public TestsetEnsureService(
        IConfiguration configuration ,
        ILogger<TestsetEnsureService> logger)
    {
        _logger = logger;
        _scriptPath =
            configuration["Judge:EnsureScriptPath"]
            ?? "/var/lib/tmoj/ensure_testset.sh";
    }

    public async Task EnsureAsync(
        string slug ,
        Guid testsetId ,
        Guid problemId ,
        CancellationToken ct)
    {
        if ( string.IsNullOrWhiteSpace(slug) )
            throw new InvalidOperationException("Problem slug is required.");

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash" ,
            Arguments = $"\"{_scriptPath}\" \"{slug}\" \"{testsetId}\" \"{problemId}\"" ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        _logger.LogInformation(
            "Ensuring testset via script. Slug={Slug}, TestsetId={TestsetId}, ProblemId={ProblemId}, Script={ScriptPath}" ,
            slug , testsetId , problemId , _scriptPath);

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if ( !string.IsNullOrWhiteSpace(stdout) )
            _logger.LogInformation("ensure_testset stdout: {Stdout}" , stdout);

        if ( !string.IsNullOrWhiteSpace(stderr) )
            _logger.LogWarning("ensure_testset stderr: {Stderr}" , stderr);

        if ( process.ExitCode != 0 )
        {
            throw new InvalidOperationException(
                $"ensure_testset failed with exit code {process.ExitCode}. stdout={stdout} stderr={stderr}");
        }
    }
}