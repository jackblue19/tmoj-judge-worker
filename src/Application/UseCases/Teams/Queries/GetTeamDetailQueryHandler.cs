using Application.Common.Interfaces;
using Application.UseCases.Teams.Dtos;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetTeamDetailHandler : IRequestHandler<GetTeamDetailQuery, TeamDto>
{
    private readonly ITeamRepository _repo;

    public GetTeamDetailHandler(ITeamRepository repo)
    {
        _repo = repo;
    }

    public async Task<TeamDto> Handle(GetTeamDetailQuery request, CancellationToken ct)
    {
        var team = await _repo.GetTeamDetailAsync(request.TeamId);

        if (team == null)
            throw new Exception("Team not found");

        return team;
    }
}