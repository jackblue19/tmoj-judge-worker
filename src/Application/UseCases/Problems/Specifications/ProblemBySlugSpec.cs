using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemBySlugSpec : Specification<Problem>
{
    public ProblemBySlugSpec(string slug, Guid? excludingProblemId = null)
    {
        var normalizedSlug = slug.Trim().ToLower();

        Query.Where(x => x.Slug != null && x.Slug.ToLower() == normalizedSlug);

        if (excludingProblemId.HasValue)
        {
            Query.Where(x => x.Id != excludingProblemId.Value);
        }
    }
}
