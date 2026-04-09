using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestTeamByUserSpec : Specification<ContestTeam>
{
    public ContestTeamByUserSpec(Guid contestId, Guid userId)
    {
        Query.Where(ct =>
            ct.ContestId == contestId &&
            ct.Team.TeamMembers.Any(tm => tm.UserId == userId)
        );
    }
}