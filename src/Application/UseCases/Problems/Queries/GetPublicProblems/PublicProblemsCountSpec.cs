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
    }
}