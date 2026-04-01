using Contracts.Submissions.Judging;
using Microsoft.Extensions.Logging;
using Worker.Execution;
using Worker.Services;

namespace Worker.Orchestration;

public sealed class SubmissionProcessor
{
    private readonly ILogger<SubmissionProcessor> _logger;
    private readonly JudgeEngine _judgeEngine;
    private readonly JudgeBackendClient _backendClient;

    public SubmissionProcessor(
        ILogger<SubmissionProcessor> logger ,
        JudgeEngine judgeEngine ,
        JudgeBackendClient backendClient)
    {
        _logger = logger;
        _judgeEngine = judgeEngine;
        _backendClient = backendClient;
    }

    public async Task ProcessAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Start processing job. JobId={JobId}, JudgeRunId={JudgeRunId}, SubmissionId={SubmissionId}" ,
            job.JobId , job.JudgeRunId , job.SubmissionId);

        var result = await _judgeEngine.ExecuteAsync(job , ct);

        await _backendClient.CompleteAsync(result , ct);

        _logger.LogInformation(
            "Finished processing job. JobId={JobId}, SubmissionId={SubmissionId}, Status={Status}, Verdict={Verdict}" ,
            result.JobId , result.SubmissionId , result.Status , result.Summary.Verdict);
    }
}