using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Mappings;

public sealed record ProblemListItemDto
{
    public Guid Id { get; init; }
    public string? Slug { get; init; }
    public string Title { get; init; } = default!;
    public string? Difficulty { get; init; }
    public string StatusCode { get; init; } = default!;
    public decimal? AcceptancePercent { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }

    public long SubmissionsCount { get; init; }
    public long AcceptedCount { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}
