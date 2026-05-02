using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestTeamsSpec : Specification<ContestTeam>
{
    public ContestTeamsSpec(Guid contestId)
    {
        Query
            .Where(x => x.ContestId == contestId)
            .Include(x => x.Team)
                .ThenInclude(t => t.TeamMembers)
                    .ThenInclude(m => m.User)
                        .ThenInclude(u => u.UserInventories)
                            .ThenInclude(ui => ui.Item);
    }
}