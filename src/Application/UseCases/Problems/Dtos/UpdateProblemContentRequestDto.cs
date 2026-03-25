using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Dtos;

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
