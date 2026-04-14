using Application.UseCases.Teams.Dtos;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetTeamDetailQuery : IRequest<TeamDto>
{
    public Guid TeamId { get; set; }

    public GetTeamDetailQuery(Guid teamId)
    {
        TeamId = teamId;
    }
}