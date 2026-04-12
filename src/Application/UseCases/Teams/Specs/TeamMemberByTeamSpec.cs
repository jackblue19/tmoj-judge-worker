using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Teams.Specs;

public class TeamMemberByTeamSpec : Specification<TeamMember>
{
    public TeamMemberByTeamSpec(Guid teamId)
    {
        Query.Where(x => x.TeamId == teamId);
    }
}