namespace Contracts.Submissions.Judging;

public sealed class JudgeExecutionOptionsContract
{
    public int TimeLimitMs { get; init; }
    public int MemoryLimitKb { get; init; }

    public string CompareMode { get; init; } = "trim";
    public bool StopOnFirstFail { get; init; } = true;
}