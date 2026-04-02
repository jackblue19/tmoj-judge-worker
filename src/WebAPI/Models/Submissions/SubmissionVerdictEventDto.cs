namespace WebAPI.Models.Submissions;

public sealed class SubmissionVerdictEventDto
{
    public Guid SubmissionId { get; set; }
    public Guid UserId { get; set; }

    public string StatusCode { get; set; } = null!;
    public string? VerdictCode { get; set; }

    public decimal? FinalScore { get; set; }
    public int? TimeMs { get; set; }
    public int? MemoryKb { get; set; }

    public DateTime? JudgedAt { get; set; }
}