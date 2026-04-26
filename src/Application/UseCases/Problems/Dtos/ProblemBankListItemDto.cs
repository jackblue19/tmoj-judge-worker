using Application.UseCases.Problems.Dtos;

namespace Application.UseCases.Problems.Queries.GetProblemBanks;

public sealed class ProblemBankListItemDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? ScoringCode { get; init; }

    public string? ProblemMode { get; init; }
    public string? ProblemSource { get; init; }

    public decimal? AcceptancePercent { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public int? DisplayIndex { get; init; }
    public DateTime? PublishedAt { get; init; }

    public IReadOnlyList<ProblemTagDto> Tags { get; init; } = [];
}