namespace WebAPI.Models.Submissions;

public sealed class SubmissionListItemDto
{
    public Guid SubmissionId { get; set; }
    public Guid UserId { get; set; }
    public Guid ProblemId { get; set; }
    public Guid? RuntimeId { get; set; }

    public string StatusCode { get; set; } = null!;
    public string? VerdictCode { get; set; }

    public decimal? FinalScore { get; set; }
    public int? TimeMs { get; set; }
    public int? MemoryKb { get; set; }

    public string? RuntimeName { get; set; }
    public string? RuntimeVersion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? JudgedAt { get; set; }
}