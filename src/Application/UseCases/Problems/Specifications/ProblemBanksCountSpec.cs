using Ardalis.Specification;
using Domain.Constants;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetProblemBanks;

public sealed class ProblemBanksCountSpec : Specification<Problem>
{
    public ProblemBanksCountSpec(string? search , string? difficulty)
    {
        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == ProblemStatusCodes.Published &&
            x.VisibilityCode == ProblemVisibilityCodes.InBank);

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