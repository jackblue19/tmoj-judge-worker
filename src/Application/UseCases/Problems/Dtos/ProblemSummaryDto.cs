namespace Application.UseCases.Problems.Dtos;

public sealed class ProblemSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }

    public string StatusCode { get; init; } = string.Empty;
    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? ScoringCode { get; init; }

    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }

    public IReadOnlyList<ProblemTagDto> Tags { get; init; } = [];
}