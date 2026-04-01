using Contracts.Submissions.Judging;

namespace Worker.Execution.Runtimes;

public interface IRuntimeExecutor
{
    bool CanHandle(DispatchJudgeJobContract job);

    Task<JudgeJobCompletedContract> ExecuteAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct);
}