using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestProblemByContestSpec : Specification<ContestProblem>
{
    public ContestProblemByContestSpec(Guid contestId)
    {
        Query
            .Where(x => x.ContestId == contestId)
            .Include(x => x.Problem);
    }
}