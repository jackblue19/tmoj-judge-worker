using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class PublicProblemsPagedSpec : Specification<Problem , PublicProblemListItemDto>
{
    public PublicProblemsPagedSpec(
        int page ,
        int pageSize ,
        string? search ,
        string? difficulty)
    {
        if ( page < 1 ) page = 1;
        if ( pageSize < 1 ) pageSize = 1;

        var skip = (page - 1) * pageSize;

        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == "published" &&
            x.VisibilityCode == "public");

        if ( !string.IsNullOrWhiteSpace(search) )
        {
            var keyword = search.Trim();

            Query.Where(x =>
                x.Title.Contains(keyword) ||
                (x.Slug != null && x.Slug.Contains(keyword)));
        }

        if ( !string.IsNullOrWhiteSpace(difficulty) )
        {
            var normalizedDifficulty = difficulty.Trim();
            Query.Where(x => x.Difficulty == normalizedDifficulty);
        }

        Query
            .OrderBy(x => x.DisplayIndex ?? int.MaxValue)
            .ThenByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize);

        Query.Select(x => new PublicProblemListItemDto(
            x.Id ,
            x.Slug ?? string.Empty ,
            x.Title ,
            x.Difficulty ,
            x.TypeCode ,
            x.AcceptancePercent ,
            x.TimeLimitMs ,
            x.MemoryLimitKb ,
            x.DisplayIndex ,
            x.PublishedAt ,
            x.Tags
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new ProblemTagsDto(
                    t.Id ,
                    t.Name ,
                    t.Slug))
                .ToList()
        ));
    }
}
