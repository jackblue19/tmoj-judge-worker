using Application.Common.Interfaces;
using Application.UseCases.Teams.Dtos;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetTeamsHandler : IRequestHandler<GetTeamsQuery , List<TeamDto>>
{
    private readonly ITeamRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetTeamsHandler(
        ITeamRepository repo ,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<TeamDto>> Handle(GetTeamsQuery request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated )
            throw new UnauthorizedAccessException();

        // 🔥 ADMIN → lấy tất cả team
        if ( _currentUser.IsInRole("Admin") || _currentUser.IsInRole("admin") )
        {
            return await _repo.GetAllTeamsAsync();
        }

        // 🔥 USER → chỉ lấy team của mình
        var userId = _currentUser.UserId!.Value;

        return await _repo.GetTeamsByUserAsync(userId);
    }
}