using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Dtos;

public sealed class CreateRemixProblemRequestDto
{
    public Guid? OriginProblemId { get; set; }
    public string? OriginProblemSlug { get; set; }

    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Difficulty { get; set; }
    public string? TypeCode { get; set; }
    public string? VisibilityCode { get; set; }
    public string? ScoringCode { get; set; }
    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }

    public string? DescriptionMd { get; set; }
    public IFormFile? StatementFile { get; set; }

    public IReadOnlyCollection<Guid>? TagIds { get; set; }
    public string? ProblemMode { get; set; }
}