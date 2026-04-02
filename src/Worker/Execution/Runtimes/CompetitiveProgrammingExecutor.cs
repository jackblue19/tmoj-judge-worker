using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Worker.Execution.Containers;
using Worker.Execution.Runtimes.Cp;
using Worker.Execution.Testset;

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
        var runtime = job.RuntimeName.Trim().ToLowerInvariant();

        return runtime.Contains("c++")
            || runtime.Contains("cpp")
            || runtime.Contains("java")
            || runtime.Contains("python")
            || runtime.Contains("prf")
            || runtime.Contains("pro")
            || runtime.Contains("pfp");
    }

    public async Task<JudgeJobCompletedContract> ExecuteAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        //var profile = ResolveProfile(job.RuntimeName);
        var profile = _profileRegistry.Resolve(job.RuntimeName);
        var workDir = CreateWorkDir(job.SubmissionId);

        var shouldCleanup = true;
        try
        {
            _logger.LogInformation(
                "CP executor start. SubmissionId={SubmissionId}, Runtime={RuntimeName}, TestsetId={TestsetId}" ,
                job.SubmissionId , job.RuntimeName , job.TestsetId);

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

                if ( compileResult.TimedOut || compileResult.ExitCode != 0 )
                    return BuildCompileError(job , compileResult);

                //  logs bugs
                var exePath = Path.Combine(workDir , "main");
                if ( profile.HasCompileStep && !File.Exists(exePath) )
                {
                    throw new InvalidOperationException(
                        $"Compiled binary not found at {exePath}");
                }

                if ( !File.Exists(exePath) )
                {
                    throw new InvalidOperationException(
                        $"Compiled binary not found at {exePath}. Compile step likely failed silently.");
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
                         Entrypoint = "/bin/bash" , // FIX
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

                if ( !File.Exists(prepared.ActualPath) )
                {
                    throw new InvalidOperationException(
                        $"Execution produced no output file: {prepared.ActualPath}. " +
                        $"ExitCode={runResult.ExitCode}, TimedOut={runResult.TimedOut}, " +
                        $"Stdout=[{runResult.Stdout}], Stderr=[{runResult.Stderr}]");
                }

                var actualOutput = await File.ReadAllTextAsync(prepared.ActualPath , ct);
                var expectedOutput = await File.ReadAllTextAsync(prepared.ExpectedPath , ct);

                var verdict = DetermineVerdict(runResult , expectedOutput , actualOutput);

                if ( verdict == "wa" )
                {
                    _logger.LogWarning(
                        "WA detected. SubmissionId={SubmissionId}, Ordinal={Ordinal}, Expected={Expected}, Actual={Actual}" ,
                        job.SubmissionId ,
                        c.Ordinal ,
                        Normalize(expectedOutput) ,
                        Normalize(actualOutput));
                }

                caseResults.Add(new JudgeCaseCompletedContract
                {
                    TestcaseId = c.TestcaseId ,
                    Ordinal = c.Ordinal ,
                    Verdict = verdict ,
                    ExitCode = runResult.ExitCode ,
                    TimedOut = runResult.TimedOut ,
                    TimeMs = runResult.ElapsedMs ,
                    MemoryKb = null ,
                    Stdout = runResult.Stdout ,
                    Stderr = runResult.Stderr ,
                    ActualOutput = actualOutput ,
                    ExpectedOutput = expectedOutput ,
                    CheckerMessage = verdict == "wa" ? "Wrong Answer" : null ,
                    Message = verdict ,
                    Note = verdict == "wa"
                            ? $"Expected(normalized)=[{Normalize(expectedOutput)}] | Actual(normalized)=[{Normalize(actualOutput)}]"
                            : null
                });

                if ( job.StopOnFirstFail && verdict != "ac" )
                    break;
            }

            var passed = caseResults.Count(x => x.Verdict == "ac");
            var finalVerdict = caseResults.Any(x => x.Verdict != "ac")
                ? caseResults.First(x => x.Verdict != "ac").Verdict
                : "ac";
            if ( finalVerdict != "ac" )
                shouldCleanup = false;

            var finalScore = job.Cases.Count == 0
                ? 0m
                : Math.Round((decimal) passed * 100m / job.Cases.Count , 2);

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
            //try { Directory.Delete(workDir , recursive: true); } catch { }
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
        string actual)
    {
        if ( runResult.TimedOut )
            return "tle";

        if ( runResult.ExitCode != 0 )
            return "re";

        return CompareOutputs(expected , actual) ? "ac" : "wa";
    }

    private static bool CompareOutputs(string expected , string actual)
    {
        return Normalize(expected) == Normalize(actual);
    }

    private static string Normalize(string value)
    {
        return string.Join('\n' ,
            value.Replace("\r\n" , "\n")
                 .Replace("\r" , "\n")
                 .Split('\n')
                 .Select(x => x.TrimEnd()))
            .Trim();
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
        return new JudgeJobCompletedContract
        {
            JobId = job.JobId ,
            JudgeRunId = job.JudgeRunId ,
            SubmissionId = job.SubmissionId ,
            WorkerId = job.WorkerId ,

            Status = "failed" ,

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
                // CE vẫn đúng ở verdict
                Verdict = "ce" ,
                Passed = 0 ,
                Total = job.Cases.Count ,
                TimeMs = compile.ElapsedMs ,
                MemoryKb = 0 ,
                FinalScore = 0
            } ,
            Cases = new List<JudgeCaseCompletedContract>()
        };
    }

}