using Application.Common.Interfaces;
using Application.UseCases.Teams.Dtos;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetTeamsHandler : IRequestHandler<GetTeamsQuery, List<TeamDto>>
{
    private readonly ITeamRepository _repo;

    public GetTeamsHandler(ITeamRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<TeamDto>> Handle(GetTeamsQuery request, CancellationToken ct)
    {
        return await _repo.GetTeamsByUserAsync(Guid.Empty);
        // NOTE: nếu muốn global list thì mình sửa lại repo sau
    }
}