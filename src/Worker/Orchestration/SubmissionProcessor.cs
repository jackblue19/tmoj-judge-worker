using Contracts.Submissions.Judging;
using Microsoft.Extensions.Logging;
using Worker.Execution.Runtimes;

namespace Worker.Orchestration;

public sealed class SubmissionProcessor
{
    private readonly ILogger<SubmissionProcessor> _logger;
    private readonly IEnumerable<IRuntimeExecutor> _executors;

    public SubmissionProcessor(
        ILogger<SubmissionProcessor> logger ,
        IEnumerable<IRuntimeExecutor> executors)
    {
        _logger = logger;
        _executors = executors;
    }

    public async Task<JudgeJobCompletedContract> ProcessAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing JobId={JobId}, SubmissionId={SubmissionId}, Runtime={RuntimeName}" ,
            job.JobId , job.SubmissionId , job.RuntimeName);

        var executor = _executors.FirstOrDefault(x => x.CanHandle(job));
        if ( executor is null )
        {
            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "failed" ,
                Note = $"No runtime executor found for runtime '{job.RuntimeName}'." ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = false ,
                    ExitCode = -1 ,
                    TimeMs = 0 ,
                    Stdout = "" ,
                    Stderr = "Unsupported runtime."
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

        return await executor.ExecuteAsync(job , ct);
    }
}