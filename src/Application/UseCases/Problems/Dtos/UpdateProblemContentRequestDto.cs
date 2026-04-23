using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Dtos;

/// <summary>
/// version 2
/// </summary>
public sealed class UpsertProblemContentRequestDto
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Difficulty { get; set; }
    public string? TypeCode { get; set; }
    public string? ScoringCode { get; set; }
    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }
    public string? DescriptionMd { get; set; }
    public IFormFile? StatementFile { get; set; }
    public IReadOnlyCollection<Guid>? TagIds { get; set; }

    // amateur | pro
    public string? ProblemMode { get; set; }
}
