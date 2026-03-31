using Contracts.Submissions.Judging;
using Microsoft.Extensions.Logging;
using Worker.Execution.Runtimes;

namespace Worker.Execution;

public sealed class JudgeEngine
{
    private readonly ILogger<JudgeEngine> _logger;
    private readonly IEnumerable<IRuntimeExecutor> _executors;

    public JudgeEngine(
        ILogger<JudgeEngine> logger ,
        IEnumerable<IRuntimeExecutor> executors)
    {
        _logger = logger;
        _executors = executors;
    }

    public async Task<JudgeJobCompletedContract> ExecuteAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "JudgeEngine received job. JobId={JobId}, SubmissionId={SubmissionId}, Runtime={RuntimeName}" ,
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