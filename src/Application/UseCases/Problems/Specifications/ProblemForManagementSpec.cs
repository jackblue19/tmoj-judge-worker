using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemForManagementSpec : Specification<Problem>
{
    public ProblemForManagementSpec(Guid problemId)
    {
        Query
            .Where(x => x.Id == problemId)
            .Include(x => x.Tags);
    }
}
