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
            ?? "vnoj/judge-tiericpc:amd64-latest";
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

            var preparedCases = new List<PreparedJudgeCaseLayout>();
            foreach ( var @case in job.Cases.OrderBy(x => x.Ordinal) )
            {
                var prepared = await _layoutAdapter.PrepareCaseAsync(
                    job.ProblemSlug ,
                    job.TestsetId ,
                    @case ,
                    workDir ,
                    ct);

                preparedCases.Add(prepared);
            }

            // TODO batch 3:
            // - resolve runtime profile (cpp/java/python)
            // - write source file
            // - compile in container
            // - run each testcase in container
            // - diff actual vs expected
            // - aggregate verdict

            // temporary placeholder docker ping
            var ping = await _dockerRunner.RunAsync(
                new DockerRunRequest
                {
                    Image = _competitiveImage ,
                    TimeoutMs = 5000 ,
                    Command = "sh -lc \"echo judge-runtime-ok\""
                } ,
                ct);

            if ( ping.TimedOut || ping.ExitCode != 0 )
            {
                return new JudgeJobCompletedContract
                {
                    JobId = job.JobId ,
                    JudgeRunId = job.JudgeRunId ,
                    SubmissionId = job.SubmissionId ,
                    WorkerId = job.WorkerId ,
                    Status = "failed" ,
                    Note = $"Runtime container bootstrap failed. exit={ping.ExitCode}" ,
                    Compile = new JudgeCompileResultContract
                    {
                        Ok = false ,
                        ExitCode = ping.ExitCode ,
                        TimeMs = ping.ElapsedMs ,
                        Stdout = ping.Stdout ,
                        Stderr = ping.Stderr
                    } ,
                    Summary = new JudgeSummaryResultContract
                    {
                        Verdict = "ie" ,
                        Passed = 0 ,
                        Total = job.Cases.Count ,
                        TimeMs = ping.ElapsedMs ,
                        MemoryKb = 0 ,
                        FinalScore = 0
                    } ,
                    Cases = new List<JudgeCaseCompletedContract>()
                };
            }

            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "failed" ,
                Note = "CompetitiveProgrammingExecutor skeleton only. Compile/run pipeline not wired yet." ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = false ,
                    ExitCode = -1 ,
                    TimeMs = 0 ,
                    Stdout = ping.Stdout ,
                    Stderr = "Compile/run not implemented yet."
                } ,
                Summary = new JudgeSummaryResultContract
                {
                    Verdict = "ie" ,
                    Passed = 0 ,
                    Total = preparedCases.Count ,
                    TimeMs = 0 ,
                    MemoryKb = 0 ,
                    FinalScore = 0
                } ,
                Cases = new List<JudgeCaseCompletedContract>()
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

    private string CreateWorkDir(Guid submissionId)
    {
        var dir = Path.Combine(
            _runtimeWorkRoot ,
            "cp" ,
            $"{submissionId:N}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(dir);
        return dir;
    }
}