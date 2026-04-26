using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestProblemSpec : Specification<ContestProblem>
{
    public ContestProblemSpec(Guid contestId)
    {
        Query.Where(x => x.ContestId == contestId)
             .Include(x => x.Problem);
    }
}
