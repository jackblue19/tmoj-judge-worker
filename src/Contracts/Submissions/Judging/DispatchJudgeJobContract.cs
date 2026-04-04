namespace Contracts.Submissions.Judging;

public sealed class DispatchJudgeJobContract
{
    public Guid JobId { get; init; }
    public Guid JudgeRunId { get; init; }
    public Guid SubmissionId { get; init; }
    public Guid? WorkerId { get; init; }

    public Guid ProblemId { get; init; }
    public string ProblemSlug { get; init; } = null!;
    public Guid TestsetId { get; init; }

    public Guid RuntimeId { get; init; }
    public string RuntimeName { get; init; } = null!;
    public string? RuntimeVersion { get; init; }
    public string RuntimeProfileKey { get; init; } = null!;
    public string? RuntimeImage { get; init; }

    public string SourceFileName { get; init; } = null!;
    public bool HasCompileStep { get; init; }
    public string CompileCommand { get; init; } = "";
    public string RunCommand { get; init; } = null!;

    public int TimeLimitMs { get; init; }
    public int MemoryLimitKb { get; init; }

    public string CompareMode { get; init; } = "trim";
    public bool StopOnFirstFail { get; init; } = true;

    public string SourceCode { get; init; } = null!;

    public List<DispatchJudgeCaseContract> Cases { get; init; } = new();
}

public sealed class DispatchJudgeCaseContract
{
    public Guid TestcaseId { get; init; }
    public int Ordinal { get; init; }
    public int Weight { get; init; }
    public bool IsSample { get; init; }
}