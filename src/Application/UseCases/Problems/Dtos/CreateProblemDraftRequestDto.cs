using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Dtos;

public sealed class CreateProblemDraftRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? StatusCode { get; init; }

    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public string? DescriptionMd { get; init; }

    public IReadOnlyCollection<Guid> TagIds { get; init; } = [];
}

public sealed class CreateProblemDraftFormDto
{
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Difficulty { get; init; }
    public string? TypeCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? StatusCode { get; init; }

    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }

    public string? DescriptionMd { get; init; }
    public IFormFile? StatementFile { get; init; }

    public List<Guid>? TagIds { get; init; }
}