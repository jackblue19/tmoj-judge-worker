using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestTeamsWithMembersSpec : Specification<ContestTeam>
{
    public ContestTeamsWithMembersSpec(Guid contestId)
    {
        Query
            .Where(x => x.ContestId == contestId)
            .Include(x => x.Team)
                .ThenInclude(t => t.TeamMembers)
                    .ThenInclude(tm => tm.User);
    }
}
