namespace Application.UseCases.ProblemSolved.Dtos;

public sealed class ProblemSolvedListDto
{
    public Guid UserId { get; init; }

    public string? VisibilityCode { get; init; }
    public string? SolvedSourceCode { get; init; }

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public IReadOnlyList<ProblemSolvedItemDto> Items { get; init; }
        = Array.Empty<ProblemSolvedItemDto>();
}

public sealed class ProblemSolvedItemDto
{
    public Guid ProblemId { get; init; }

    public string? Slug { get; init; }
    public string Title { get; init; } = string.Empty;

    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? StatusCode { get; init; }

    public string? ProblemMode { get; init; }
    public string? ProblemSource { get; init; }

    public int AcceptedSubmissionsCount { get; init; }

    public DateTime FirstSolvedAt { get; init; }
    public DateTime LastSolvedAt { get; init; }

    public IReadOnlyList<string> SolvedSourceCodes { get; init; }
        = Array.Empty<string>();

    public Guid? BestSubmissionId { get; init; }
    public int? BestTimeMs { get; init; }
    public int? BestMemoryKb { get; init; }
}