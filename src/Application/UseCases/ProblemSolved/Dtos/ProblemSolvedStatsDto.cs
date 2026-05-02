namespace Application.UseCases.ProblemSolved.Dtos;

public sealed class ProblemSolvedStatsDto
{
    public Guid UserId { get; init; }

    public string? VisibilityCode { get; init; }
    public string? SolvedSourceCode { get; init; }

    public int TotalSolved { get; init; }

    public IReadOnlyList<ProblemSolvedGroupDto> ByVisibility { get; init; }
        = Array.Empty<ProblemSolvedGroupDto>();

    public IReadOnlyList<ProblemSolvedGroupDto> BySource { get; init; }
        = Array.Empty<ProblemSolvedGroupDto>();
}

public sealed class ProblemSolvedGroupDto
{
    public string Code { get; init; } = string.Empty;
    public int Count { get; init; }
}