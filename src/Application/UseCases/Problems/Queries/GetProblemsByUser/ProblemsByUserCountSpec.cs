using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed class ProblemsByUserCountSpec : Specification<UserProblemStat>
{
    public ProblemsByUserCountSpec(Guid userId)
    {
        Query.Where(x => x.UserId == userId);
    }
}