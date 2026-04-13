using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemWithTagsAndTestsetsSpec : Specification<Problem>
{
    public ProblemWithTagsAndTestsetsSpec(Guid problemId)
    {
        Query.Where(x => x.Id == problemId)
             .Include(x => x.Tags)
             .Include(x => x.Testsets);
    }
}