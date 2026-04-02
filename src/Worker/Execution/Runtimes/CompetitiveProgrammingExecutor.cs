using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Worker.Execution.Containers;
using Worker.Execution.Runtimes.Cp;
using Worker.Execution.Testset;
using Worker.Execution.Utils;

namespace Worker.Execution.Runtimes;

public sealed class CompetitiveProgrammingExecutor : IRuntimeExecutor
{
    private readonly ILogger<CompetitiveProgrammingExecutor> _logger;
    private readonly TestsetEnsureService _ensureService;
    private readonly TestsetLayoutAdapter _layoutAdapter;
    private readonly DockerSandboxRunner _dockerRunner;
    private readonly string _runtimeWorkRoot;
    private readonly string _competitiveImage;
    private readonly RuntimeProfileRegistry _profileRegistry;

    public CompetitiveProgrammingExecutor(
        IConfiguration configuration ,
        ILogger<CompetitiveProgrammingExecutor> logger ,
        TestsetEnsureService ensureService ,
        TestsetLayoutAdapter layoutAdapter ,
        DockerSandboxRunner dockerRunner ,
        RuntimeProfileRegistry profileRegistry)
    {
        _logger = logger;
        _ensureService = ensureService;
        _layoutAdapter = layoutAdapter;
        _dockerRunner = dockerRunner;
        _profileRegistry = profileRegistry;

        _runtimeWorkRoot =
            configuration["Judge:RuntimeWorkRoot"]
            ?? "/var/lib/tmoj/runtime";

        _competitiveImage =
            configuration["Docker:CompetitiveImage"]
            ?? "vnoj/runtimes-tier3:latest";
    }

    public bool CanHandle(DispatchJudgeJobContract job)
    {
        if ( !string.IsNullOrWhiteSpace(job.RuntimeProfileKey) )
            return true;

        var runtime = job.RuntimeName.Trim().ToLowerInvariant();

        return runtime.Contains("c++")
            || runtime.Contains("cpp")
            || runtime.Contains("java")
            || runtime.Contains("python");
    }

    public async Task<JudgeJobCompletedContract> ExecuteAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        var profile = _profileRegistry.Resolve(job.RuntimeProfileKey);
        var workDir = CreateWorkDir(job.SubmissionId);

        var shouldCleanup = true;

        try
        {
            _logger.LogInformation(
                "CP executor start. SubmissionId={SubmissionId}, RuntimeProfileKey={RuntimeProfileKey}, TestsetId={TestsetId}, TimeLimitMs={TimeLimitMs}, MemoryLimitKb={MemoryLimitKb}" ,
                job.SubmissionId , job.RuntimeProfileKey , job.TestsetId , job.TimeLimitMs , job.MemoryLimitKb);

            await _ensureService.EnsureAsync(
                job.ProblemSlug ,
                job.TestsetId ,
                job.ProblemId ,
                ct);

            var sourcePath = Path.Combine(workDir , profile.SourceFileName);
            await File.WriteAllTextAsync(sourcePath , job.SourceCode , ct);

            DockerRunResult? compileResult = null;

            if ( profile.HasCompileStep )
            {
                compileResult = await _dockerRunner.RunAsync(
                    new DockerRunRequest
                    {
                        Image = ResolveImage(job) ,
                        Entrypoint = "/bin/bash" ,
                        WorkingDirectory = "/work" ,
                        TimeoutMs = Math.Max(job.TimeLimitMs * 2 , 5000) ,
                        MemoryLimit = ResolveMemoryLimit(job.MemoryLimitKb) ,
                        CpuLimit = "1.0" ,
                        Mounts = new()
                        {
                            new DockerMount { HostPath = workDir, ContainerPath = "/work" }
                        } ,
                        Command = $"-lc \"{profile.CompileCommand}\""
                    } ,
                    ct);

                if ( compileResult.TimedOut || compileResult.ExitCode != 0 || compileResult.OomKilled )
                    return BuildCompileError(job , compileResult);

                var artifactPath = ResolveCompiledArtifactPath(workDir , profile);
                if ( !string.IsNullOrWhiteSpace(artifactPath) && !File.Exists(artifactPath) )
                {
                    throw new InvalidOperationException(
                        $"Compiled artifact not found at {artifactPath}");
                }
            }

            var caseResults = new List<JudgeCaseCompletedContract>();
            var totalTimeMs = 0;
            int? peakMemoryKb = null;

            foreach ( var c in job.Cases.OrderBy(x => x.Ordinal) )
            {
                var prepared = await _layoutAdapter.PrepareCaseAsync(
                    job.ProblemSlug ,
                    job.TestsetId ,
                    c ,
                    workDir ,
                    ct);

                var runResult = await _dockerRunner.RunAsync(
                    new DockerRunRequest
                    {
                        Image = ResolveImage(job) ,
                        Entrypoint = "/bin/bash" ,
                        WorkingDirectory = "/work" ,
                        TimeoutMs = Math.Max(job.TimeLimitMs , 1000) ,
                        MemoryLimit = ResolveMemoryLimit(job.MemoryLimitKb) ,
                        CpuLimit = "1.0" ,
                        Mounts = new()
                        {
                            new DockerMount { HostPath = workDir, ContainerPath = "/work" }
                        } ,
                        Command = BuildRunCommand(profile , prepared)
                    } ,
                    ct);

                totalTimeMs += runResult.ElapsedMs;

                if ( runResult.PeakMemoryKb.HasValue )
                {
                    peakMemoryKb = !peakMemoryKb.HasValue
                        ? runResult.PeakMemoryKb.Value
                        : Math.Max(peakMemoryKb.Value , runResult.PeakMemoryKb.Value);
                }

                if ( !runResult.TimedOut && !runResult.OomKilled && !File.Exists(prepared.ActualPath) )
                {
                    throw new InvalidOperationException(
                        $"Execution produced no output file: {prepared.ActualPath}. " +
                        $"ExitCode={runResult.ExitCode}, TimedOut={runResult.TimedOut}, OomKilled={runResult.OomKilled}, " +
                        $"Stdout=[{runResult.Stdout}], Stderr=[{runResult.Stderr}]");
                }

                var actualOutput = File.Exists(prepared.ActualPath)
                    ? await File.ReadAllTextAsync(prepared.ActualPath , ct)
                    : string.Empty;

                var expectedOutput = await File.ReadAllTextAsync(prepared.ExpectedPath , ct);

                var verdict = DetermineVerdict(
                    runResult ,
                    expectedOutput ,
                    actualOutput ,
                    job.CompareMode);

                caseResults.Add(new JudgeCaseCompletedContract
                {
                    TestcaseId = c.TestcaseId ,
                    Ordinal = c.Ordinal ,
                    Verdict = verdict ,
                    ExitCode = runResult.ExitCode ,
                    TimedOut = runResult.TimedOut ,
                    TimeMs = runResult.ElapsedMs ,
                    MemoryKb = runResult.PeakMemoryKb ,
                    Stdout = runResult.Stdout ,
                    Stderr = runResult.Stderr ,
                    ActualOutput = actualOutput ,
                    ExpectedOutput = expectedOutput ,
                    CheckerMessage = BuildCheckerMessage(verdict) ,
                    Message = verdict ,
                    Note = BuildCaseNote(verdict , expectedOutput , actualOutput , runResult)
                });

                if ( job.StopOnFirstFail && verdict != "ac" )
                    break;
            }

            var finalVerdict = caseResults.Any(x => x.Verdict != "ac")
                ? caseResults.First(x => x.Verdict != "ac").Verdict
                : "ac";

            var passed = caseResults.Count(x => x.Verdict == "ac");

            var weightByCaseId = job.Cases.ToDictionary(x => x.TestcaseId , x => x.Weight);
            var totalWeight = job.Cases.Sum(x => x.Weight);
            var passedWeight = caseResults
                .Where(x => x.Verdict == "ac")
                .Sum(x => weightByCaseId.TryGetValue(x.TestcaseId , out var w) ? w : 0);

            var finalScore = totalWeight <= 0
                ? 0m
                : Math.Round((decimal) passedWeight * 100m / totalWeight , 2);

            if ( finalVerdict != "ac" )
                shouldCleanup = false;

            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "done" ,
                Note = null ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = true ,
                    ExitCode = 0 ,
                    TimeMs = compileResult?.ElapsedMs ,
                    Stdout = compileResult?.Stdout ?? "" ,
                    Stderr = compileResult?.Stderr ?? ""
                } ,
                Summary = new JudgeSummaryResultContract
                {
                    Verdict = finalVerdict ,
                    Passed = passed ,
                    Total = job.Cases.Count ,
                    TimeMs = totalTimeMs ,
                    MemoryKb = peakMemoryKb ,
                    FinalScore = finalScore
                } ,
                Cases = caseResults
            };
        }
        catch ( Exception ex )
        {
            _logger.LogError(ex ,
                "CP executor crashed. SubmissionId={SubmissionId}, JobId={JobId}" ,
                job.SubmissionId , job.JobId);

            shouldCleanup = false;

            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "failed" ,
                Note = ex.Message ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = false ,
                    ExitCode = -1 ,
                    TimeMs = 0 ,
                    Stdout = "" ,
                    Stderr = ex.ToString()
                } ,
                Summary = new JudgeSummaryResultContract
                {
                    Verdict = "ie" ,
                    Passed = 0 ,
                    Total = job.Cases.Count ,
                    TimeMs = 0 ,
                    MemoryKb = 0 ,
                    FinalScore = 0
                } ,
                Cases = new List<JudgeCaseCompletedContract>()
            };
        }
        finally
        {
            if ( shouldCleanup )
            {
                try { Directory.Delete(workDir , recursive: true); } catch { }
            }
            else
            {
                _logger.LogWarning(
                    "Keeping workDir for debugging. SubmissionId={SubmissionId}, WorkDir={WorkDir}" ,
                    job.SubmissionId , workDir);
            }
        }
    }

    private string ResolveImage(DispatchJudgeJobContract job)
    {
        return string.IsNullOrWhiteSpace(job.RuntimeImage)
            ? _competitiveImage
            : job.RuntimeImage!;
    }

    private static string ResolveMemoryLimit(int memoryKb)
    {
        var mb = Math.Max((int) Math.Ceiling(memoryKb / 1024.0) , 64);
        return $"{mb}m";
    }

    private static string BuildRunCommand(
        ICpExecutorProfile profile ,
        PreparedJudgeCaseLayout prepared)
    {
        var inputRelative = GetRelativeUnixPath(prepared.InputPath , GetWorkRoot(prepared.CaseDirectory));
        var outputRelative = GetRelativeUnixPath(prepared.ActualPath , GetWorkRoot(prepared.CaseDirectory));

        return $"-lc \"{profile.RunCommand} < '{inputRelative}' > '{outputRelative}'\"";
    }

    private static string GetWorkRoot(string caseDirectory)
    {
        var dir = new DirectoryInfo(caseDirectory);
        while ( dir.Parent is not null && dir.Name != "cases" )
            dir = dir.Parent;

        return dir.Parent?.FullName ?? dir.FullName;
    }

    private static string GetRelativeUnixPath(string fullPath , string root)
    {
        var relative = Path.GetRelativePath(root , fullPath);
        return relative.Replace('\\' , '/');
    }

    private static string DetermineVerdict(
        DockerRunResult runResult ,
        string expected ,
        string actual ,
        string compareMode)
    {
        if ( runResult.TimedOut )
            return "tle";

        if ( runResult.OomKilled )
            return "mle";

        if ( runResult.ExitCode != 0 )
            return "re";

        return OutputComparer.Compare(expected , actual , compareMode) ? "ac" : "wa";
    }

    private static string? BuildCheckerMessage(string verdict)
    {
        return verdict switch
        {
            "wa" => "Wrong Answer",
            "tle" => "Time Limit Exceeded",
            "mle" => "Memory Limit Exceeded",
            "re" => "Runtime Error",
            "ce" => "Compile Error",
            _ => null
        };
    }

    private static string? BuildCaseNote(
        string verdict ,
        string expected ,
        string actual ,
        DockerRunResult runResult)
    {
        return verdict switch
        {
            "wa" => $"Expected(normalized)=[{OutputComparer.NormalizeForNote(expected)}] | Actual(normalized)=[{OutputComparer.NormalizeForNote(actual)}]",
            "tle" => "Execution exceeded time limit.",
            "mle" => "Execution exceeded memory limit.",
            "re" => string.IsNullOrWhiteSpace(runResult.Stderr) ? "Runtime error." : runResult.Stderr,
            _ => null
        };
    }

    private string CreateWorkDir(Guid submissionId)
    {
        var dir = Path.Combine(
            _runtimeWorkRoot ,
            "cp" ,
            $"{submissionId:N}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(dir);
        return dir;
    }

    private static JudgeJobCompletedContract BuildCompileError(
        DispatchJudgeJobContract job ,
        DockerRunResult compile)
    {
        var compileVerdict = compile.OomKilled ? "mle" : compile.TimedOut ? "tle" : "ce";

        return new JudgeJobCompletedContract
        {
            JobId = job.JobId ,
            JudgeRunId = job.JudgeRunId ,
            SubmissionId = job.SubmissionId ,
            WorkerId = job.WorkerId ,
            Status = "done" ,
            Note = "Compile failed." ,
            Compile = new JudgeCompileResultContract
            {
                Ok = false ,
                ExitCode = compile.ExitCode ,
                TimeMs = compile.ElapsedMs ,
                Stdout = compile.Stdout ,
                Stderr = compile.Stderr
            } ,
            Summary = new JudgeSummaryResultContract
            {
                Verdict = compileVerdict ,
                Passed = 0 ,
                Total = job.Cases.Count ,
                TimeMs = compile.ElapsedMs ,
                MemoryKb = compile.PeakMemoryKb ,
                FinalScore = 0
            } ,
            Cases = new List<JudgeCaseCompletedContract>()
        };
    }

    private static string? ResolveCompiledArtifactPath(string workDir , ICpExecutorProfile profile)
    {
        if ( string.IsNullOrWhiteSpace(profile.CompiledArtifactFileName) )
            return null;

        return Path.Combine(workDir , profile.CompiledArtifactFileName);
    }
}