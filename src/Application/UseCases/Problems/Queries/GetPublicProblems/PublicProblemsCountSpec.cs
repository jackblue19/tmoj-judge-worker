using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class PublicProblemsCountSpec : Specification<Problem>
{
    public PublicProblemsCountSpec(string? search , string? difficulty)
    {
        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == "published" &&
            x.VisibilityCode == "public");

        if ( !string.IsNullOrWhiteSpace(search) )
        {
            var keyword = search.Trim().ToLower();

            Query.Where(x =>
                (x.Title != null && x.Title.ToLower().Contains(keyword)) ||
                (x.Slug != null && x.Slug.ToLower().Contains(keyword)));
        }

        if ( !string.IsNullOrWhiteSpace(difficulty) )
        {
            Query.Where(x => x.Difficulty == difficulty);
        }

        Query.AsNoTracking();
    }
}