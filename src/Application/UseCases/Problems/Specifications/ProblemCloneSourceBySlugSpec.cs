using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemCloneSourceBySlugSpec : Specification<Problem>
{
    public ProblemCloneSourceBySlugSpec(string slug)
    {
        Query
            .Where(x => x.Slug == slug && x.IsActive)
            .Include(x => x.Tags)
            .Include(x => x.ProblemTemplates.Where(t => t.IsActive))
            .AsSplitQuery()
            .AsNoTracking();
    }
}