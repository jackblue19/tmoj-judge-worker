namespace WebAPI.Models.Submissions;

public sealed class SubmissionSearchRequest
{
    public Guid? UserId { get; set; }
    public Guid? ProblemId { get; set; }
    public Guid? RuntimeId { get; set; }

    public string? StatusCode { get; set; }
    public string? VerdictCode { get; set; }

    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}