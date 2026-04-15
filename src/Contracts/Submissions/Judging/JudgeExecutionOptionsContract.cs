namespace Contracts.Submissions.Judging;

public sealed class JudgeExecutionOptionsContract
{
    public int TimeLimitMs { get; init; }
    public int MemoryLimitKb { get; init; }

    public string CompareMode { get; init; } = "trim";

    // Default: IOI (chấm hết test case, cộng điểm theo Weight).
    // Chỉ contest ACM mới đặt = true (dừng tại test đầu FAIL).
    public bool StopOnFirstFail { get; init; } = false;
}