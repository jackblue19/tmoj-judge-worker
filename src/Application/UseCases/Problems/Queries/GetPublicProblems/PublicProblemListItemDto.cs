using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed record PublicProblemListItemDto(
    Guid Id ,
    string Slug ,
    string Title ,
    string? Difficulty ,
    string? TypeCode ,
    decimal? AcceptancePercent ,
    int? TimeLimitMs ,
    int? MemoryLimitKb ,
    int? DisplayIndex ,
    DateTime? PublishedAt ,
    IReadOnlyList<ProblemTagsDto> Tags
);
