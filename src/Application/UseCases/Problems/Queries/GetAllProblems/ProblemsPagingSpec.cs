using Application.UseCases.Problems.Mappings;
using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Queries.GetAllProblems;

public sealed class ProblemsPagingSpec
    : Specification<Problem , ProblemListItemDto>
{
    public ProblemsPagingSpec(
        string? difficulty ,
        string? status ,
        int skip ,
        int take)
    {
        Query
            .Where(x => x.IsActive)
            .Include(x => x.ProblemStat)
            .Include(x => x.Tags);

        if ( !string.IsNullOrWhiteSpace(difficulty) )
            Query.Where(x => x.Difficulty == difficulty);

        if ( !string.IsNullOrWhiteSpace(status) )
            Query.Where(x => x.StatusCode == status);

        Query
            .OrderBy(x => x.DisplayIndex)
            .Skip(skip)
            .Take(take);

        Query.Select(x => new ProblemListItemDto
        {
            Id = x.Id ,
            Slug = x.Slug ,
            Title = x.Title ,
            Difficulty = x.Difficulty ,
            StatusCode = x.StatusCode ,
            AcceptancePercent = x.AcceptancePercent ,
            TimeLimitMs = x.TimeLimitMs ,
            MemoryLimitKb = x.MemoryLimitKb ,
            CreatedAt = x.CreatedAt ,
            PublishedAt = x.PublishedAt ,

            SubmissionsCount = x.ProblemStat != null
                ? x.ProblemStat.SubmissionsCount
                : 0 ,

            AcceptedCount = x.ProblemStat != null
                ? x.ProblemStat.AcceptedCount
                : 0 ,

            Tags = x.Tags.Select(t => t.Name).ToList()
        });
    }
}
