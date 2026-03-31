using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Worker.Execution.Containers;
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

    public CompetitiveProgrammingExecutor(
        IConfiguration configuration ,
        ILogger<CompetitiveProgrammingExecutor> logger ,
        TestsetEnsureService ensureService ,
        TestsetLayoutAdapter layoutAdapter ,
        DockerSandboxRunner dockerRunner)
    {
        _logger = logger;
        _ensureService = ensureService;
        _layoutAdapter = layoutAdapter;
        _dockerRunner = dockerRunner;

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
        var profile = ResolveProfile(job.RuntimeName);
        var workDir = CreateWorkDir(job.SubmissionId);

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
                        WorkingDirectory = "/work" ,
                        TimeoutMs = Math.Max(job.TimeLimitMs * 2 , 5000) ,
                        MemoryLimit = ResolveMemoryLimit(job.MemoryLimitKb) ,
                        CpuLimit = "1.0" ,
                        Mounts = new()
                        {
                            new DockerMount { HostPath = workDir, ContainerPath = "/work" }
                        } ,
                        Command = $"bash -lc \"{profile.CompileCommand}\""
                    } ,
                    ct);

                if ( compileResult.TimedOut || compileResult.ExitCode != 0 )
                    return BuildCompileError(job , compileResult);
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

                var actualOutput = File.Exists(prepared.ActualPath)
                    ? await File.ReadAllTextAsync(prepared.ActualPath , ct)
                    : "";

                var expectedOutput = await File.ReadAllTextAsync(prepared.ExpectedPath , ct);

                var verdict = DetermineVerdict(runResult , expectedOutput , actualOutput);

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
                    ExpectedOutput = null ,
                    CheckerMessage = verdict == "wa" ? "Wrong Answer" : null ,
                    Message = verdict ,
                    Note = null
                });

                if ( job.StopOnFirstFail && verdict != "ac" )
                    break;
            }

            var passed = caseResults.Count(x => x.Verdict == "ac");
            var finalVerdict = caseResults.Any(x => x.Verdict != "ac")
                ? caseResults.First(x => x.Verdict != "ac").Verdict
                : "ac";

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
            try { Directory.Delete(workDir , recursive: true); } catch { }
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
        CpRuntimeProfile profile ,
        PreparedJudgeCaseLayout prepared)
    {
        var inputRelative = GetRelativeUnixPath(prepared.InputPath , GetWorkRoot(prepared.CaseDirectory));
        var outputRelative = GetRelativeUnixPath(prepared.ActualPath , GetWorkRoot(prepared.CaseDirectory));

        return $"bash -lc \"{profile.RunCommand} < '{inputRelative}' > '{outputRelative}'\"";
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

    private static CpRuntimeProfile ResolveProfile(string runtimeName)
    {
        var r = runtimeName.ToLowerInvariant();

        if ( r.Contains("cpp") || r.Contains("c++") || r.Contains("prf") )
        {
            return new CpRuntimeProfile
            {
                SourceFileName = "main.cpp" ,
                CompileCommand = "g++ -O2 -std=c++17 main.cpp -o main" ,
                RunCommand = "./main" ,
                HasCompileStep = true
            };
        }

        if ( r.Contains("java") || r.Contains("pro") )
        {
            return new CpRuntimeProfile
            {
                SourceFileName = "Main.java" ,
                CompileCommand = "javac Main.java" ,
                RunCommand = "java Main" ,
                HasCompileStep = true
            };
        }

        if ( r.Contains("python") || r.Contains("pfp") )
        {
            return new CpRuntimeProfile
            {
                SourceFileName = "main.py" ,
                CompileCommand = "" ,
                RunCommand = "python3 main.py" ,
                HasCompileStep = false
            };
        }

        throw new InvalidOperationException($"Unsupported competitive runtime: {runtimeName}");
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
            Status = "compile_error" ,
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

    private sealed class CpRuntimeProfile
    {
        public string SourceFileName { get; init; } = null!;
        public string CompileCommand { get; init; } = null!;
        public string RunCommand { get; init; } = null!;
        public bool HasCompileStep { get; init; }
    }
}