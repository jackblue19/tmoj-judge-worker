using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemCloneSourceByIdSpec : Specification<Problem>
{
    public ProblemCloneSourceByIdSpec(Guid problemId)
    {
        Query
            .Where(x => x.Id == problemId && x.IsActive)
            .Include(x => x.Tags)
            .Include(x => x.ProblemTemplates.Where(t => t.IsActive))
            .AsSplitQuery()
            .AsNoTracking();
    }
}