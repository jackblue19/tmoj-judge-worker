using Contracts.Submissions.Judging;
using Microsoft.Extensions.Logging;

namespace Worker.Orchestration;

public sealed class SubmissionProcessor
{
    private readonly ILogger<SubmissionProcessor> _logger;

    public SubmissionProcessor(ILogger<SubmissionProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<JudgeJobCompletedContract> ProcessAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Start processing JobId={JobId}, SubmissionId={SubmissionId}, ProblemId={ProblemId}, TestsetId={TestsetId}, Runtime={RuntimeName}" ,
            job.JobId ,
            job.SubmissionId ,
            job.ProblemId ,
            job.TestsetId ,
            job.RuntimeName);

        // batch sau:
        // 1. ensure testset
        // 2. resolve testcase file paths
        // 3. execute runtime container
        // 4. aggregate result

        await Task.Delay(10 , ct);

        return new JudgeJobCompletedContract
        {
            JobId = job.JobId ,
            JudgeRunId = job.JudgeRunId ,
            SubmissionId = job.SubmissionId ,
            WorkerId = job.WorkerId ,
            Status = "failed" ,
            Note = "SubmissionProcessor skeleton only. Execution pipeline not wired yet." ,
            Compile = new JudgeCompileResultContract
            {
                Ok = false ,
                ExitCode = -1 ,
                TimeMs = 0 ,
                Stdout = "" ,
                Stderr = "Execution pipeline not implemented yet."
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
}