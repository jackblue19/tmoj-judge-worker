using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
