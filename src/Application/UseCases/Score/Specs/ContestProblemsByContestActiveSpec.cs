using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class ContestProblemsByContestActiveSpec : Specification<ContestProblem>
{
    public ContestProblemsByContestActiveSpec(Guid contestId)
    {
        Query.Where(cp => cp.ContestId == contestId && cp.IsActive)
             .OrderBy(cp => cp.Ordinal);
    }
}
