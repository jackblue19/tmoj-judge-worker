using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetTeamInviteCodeQueryHandler : IRequestHandler<GetTeamInviteCodeQuery, string?>
{
    private readonly IReadRepository<ContestTeam, Guid> _ctRepo;
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly ICurrentUserService _currentUser;

    public GetTeamInviteCodeQueryHandler(
        IReadRepository<ContestTeam, Guid> ctRepo,
        IReadRepository<Team, Guid> teamRepo,
        ICurrentUserService currentUser)
    {
        _ctRepo = ctRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
    }

    public async Task<string?> Handle(GetTeamInviteCodeQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var contestTeam = await _ctRepo.FirstOrDefaultAsync(
            new ContestTeamByUserSpec(request.ContestId, userId), ct);

        if (contestTeam == null)
            throw new Exception("NOT_IN_CONTEST_TEAM");

        var teamEntity = await _teamRepo.GetByIdAsync(contestTeam.TeamId, ct);
        if (teamEntity == null)
             return null;

        // Only leader can see invite code? Usually yes.
        if (teamEntity.LeaderId != userId)
            throw new Exception("NOT_TEAM_LEADER");

        return teamEntity.InviteCode;
    }
}
