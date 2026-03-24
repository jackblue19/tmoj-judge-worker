namespace WebAPI.Judging;

public sealed class SubmissionRequestModel      // vnoj-docker
{
    public int SubmissionId { get; set; }
    public string ProblemId { get; set; } = default!;
    public string Language { get; set; } = default!;
    public string Source { get; set; } = default!;
    public int TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public bool ShortCircuit { get; set; }
    public string? JudgeId { get; set; }
}

public sealed class SubmissionState
{
    public int SubmissionId { get; set; }
    public string ProblemId { get; set; } = default!;
    public string Language { get; set; } = default!;
    public string Status { get; set; } = "Queued";
    public bool IsDone { get; set; }
    public bool IsPretested { get; set; }
    public int? CurrentCase { get; set; }
    public int? Batch { get; set; }
    public double? Time { get; set; }
    public int? Memory { get; set; }
    public int? Points { get; set; }
    public int? Total { get; set; }
    public string? Output { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SubmissionCaseState> Cases { get; set; } = new();
}

public sealed class SubmissionCaseState
{
    public int CaseNumber { get; set; }
    public string Status { get; set; } = default!;
    public int? Batch { get; set; }
    public double? Time { get; set; }
    public int? Memory { get; set; }
    public int? Points { get; set; }
    public int? Total { get; set; }
    public string? Output { get; set; }
}