namespace WebAPI.Models.Submissions;

public sealed class SubmissionDiagnosticDto
{
    public string Level { get; init; } = "info";
    public string Code { get; init; } = "none";
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Hints { get; init; } = Array.Empty<string>();
}

public sealed class SubmissionDetailDto
{
    public Guid SubmissionId { get; set; }
    public Guid UserId { get; set; }

    public Guid ProblemId { get; set; }
    public string? ProblemSlug { get; set; }
    public string? ProblemTitle { get; set; }
    public string? ProblemMode { get; set; }
    public string? ProblemVisibilityCode { get; set; }
    public int? ProblemTimeLimitMs { get; set; }
    public int? ProblemMemoryLimitKb { get; set; }

    public Guid? RuntimeId { get; set; }
    public string? RuntimeName { get; set; }
    public string? RuntimeVersion { get; set; }
    public string? RuntimeProfileKey { get; set; }
    public string? RuntimeSourceFileName { get; set; }
    public string? RuntimeCompileCommand { get; set; }
    public string? RuntimeRunCommand { get; set; }

    public Guid? TestsetId { get; set; }

    public string StatusCode { get; set; } = string.Empty;
    public string? VerdictCode { get; set; }

    public decimal? FinalScore { get; set; }
    public int? TimeMs { get; set; }
    public int? MemoryKb { get; set; }

    public string? SourceCode { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? JudgedAt { get; set; }

    public SubmissionRunDto? LatestRun { get; set; }

    public List<SubmissionCaseResultDto> Results { get; set; } = new();

    public SubmissionDiagnosticDto? Diagnostic { get; set; }
}