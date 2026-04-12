using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

public sealed class ContestProblemByIdWithContestSpec : Specification<ContestProblem>
{
    public ContestProblemByIdWithContestSpec(Guid contestProblemId)
    {
        Query.Where(cp => cp.Id == contestProblemId)
             .Include(cp => cp.Contest);
    }
}
