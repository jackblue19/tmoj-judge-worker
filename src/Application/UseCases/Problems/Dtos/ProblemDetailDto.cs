namespace Application.UseCases.Problems.Dtos;

public sealed class ProblemDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? DescriptionMd { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public IReadOnlyList<ProblemTagDto> Tags { get; init; } = [];
}
