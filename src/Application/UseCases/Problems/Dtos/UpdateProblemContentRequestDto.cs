using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Dtos;

/// <summary>
/// version 1
/// </summary>
public sealed class UpdateProblemContentRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? DescriptionMd { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public string? TypeCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? VisibilityCode { get; init; }
    public IFormFile? StatementFile { get; init; }
}

/// <summary>
/// version 2
/// </summary>
public sealed class UpsertProblemContentRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public string? TypeCode { get; init; }
    public string? ScoringCode { get; init; }
    public string? VisibilityCode { get; init; }

    public string? DescriptionMd { get; init; }

    public IFormFile? StatementFile { get; init; }
}
