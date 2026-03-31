namespace Contracts.Submissions.Judging;

public sealed class JudgeJobCompletedContract
{
    public Guid JobId { get; init; }
    public Guid JudgeRunId { get; init; }
    public Guid SubmissionId { get; init; }
    public Guid? WorkerId { get; init; }

    public string Status { get; init; } = null!; // done / failed / compile_error

    public JudgeCompileResultContract Compile { get; init; } = new();
    public JudgeSummaryResultContract Summary { get; init; } = new();

    public string? Note { get; init; }
    public List<JudgeCaseCompletedContract> Cases { get; init; } = new();
}

public sealed class JudgeCompileResultContract
{
    public bool Ok { get; init; }
    public int ExitCode { get; init; }
    public int? TimeMs { get; init; }
    public string Stdout { get; init; } = "";
    public string Stderr { get; init; } = "";
}

public sealed class JudgeSummaryResultContract
{
    public string Verdict { get; init; } = "ie";
    public int Passed { get; init; }
    public int Total { get; init; }
    public int? TimeMs { get; init; }
    public int? MemoryKb { get; init; }
    public decimal? FinalScore { get; init; }
}

public sealed class JudgeCaseCompletedContract
{
    public Guid TestcaseId { get; init; }
    public int Ordinal { get; init; }

    public string Verdict { get; init; } = "ie";
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public int? TimeMs { get; init; }
    public int? MemoryKb { get; init; }

    public string Stdout { get; init; } = "";
    public string Stderr { get; init; } = "";

    public string? ActualOutput { get; init; }
    public string? ExpectedOutput { get; init; }

    public string? CheckerMessage { get; init; }
    public string? Message { get; init; }
    public string? Note { get; init; }
}