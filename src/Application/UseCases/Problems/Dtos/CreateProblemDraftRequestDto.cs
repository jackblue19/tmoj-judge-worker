namespace Application.UseCases.Problems.Dtos;

public sealed class CreateProblemDraftRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public string? TypeCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? DescriptionMd { get; init; }
}
