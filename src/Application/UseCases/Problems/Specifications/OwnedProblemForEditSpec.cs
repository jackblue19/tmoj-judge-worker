using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class OwnedProblemForEditSpec : Specification<Problem>
{
    public OwnedProblemForEditSpec(Guid problemId)
    {
        Query.Where(x => x.Id == problemId)
             .Include(x => x.Tags);
    }
}
