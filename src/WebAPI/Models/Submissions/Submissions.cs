namespace WebAPI.Models.Submissions;

public sealed class SubmissionCaseResultDto
{
    public Guid ResultId { get; set; }
    public Guid? TestcaseId { get; set; }
    public int? Ordinal { get; set; }

    public string? StatusCode { get; set; }

    public int? RuntimeMs { get; set; }
    public int? MemoryKb { get; set; }

    public string? CheckerMessage { get; set; }

    public int? ExitCode { get; set; }
    public int? Signal { get; set; }

    public string? Message { get; set; }
    public string? Note { get; set; }

    public string? ExpectedOutput { get; set; }
    public string? ActualOutput { get; set; }

    public string? Type { get; set; }

    public DateTime CreatedAt { get; set; }
}