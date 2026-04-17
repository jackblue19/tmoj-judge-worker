using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class CreateTeamInviteCodeCommandHandler : IRequestHandler<CreateTeamInviteCodeCommand, string>
{
    private readonly IReadRepository<ContestTeam, Guid> _ctRepo;
    private readonly IWriteRepository<Team, Guid> _teamRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateTeamInviteCodeCommandHandler(
        IReadRepository<ContestTeam, Guid> ctRepo,
        IWriteRepository<Team, Guid> teamRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _ctRepo = ctRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<string> Handle(CreateTeamInviteCodeCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var contestTeam = await _ctRepo.FirstOrDefaultAsync(
            new ContestTeamByUserSpec(request.ContestId, userId), ct);

        if (contestTeam == null)
            throw new Exception("NOT_IN_CONTEST_TEAM");

        var teamEntity = await _teamRepo.GetByIdAsync(contestTeam.TeamId, ct);
        if (teamEntity == null)
            throw new Exception("TEAM_NOT_FOUND");

        // Check if user is leader
        if (teamEntity.LeaderId != userId)
            throw new Exception("NOT_TEAM_LEADER");

        var inviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
        teamEntity.InviteCode = inviteCode;
        teamEntity.UpdatedAt = DateTime.UtcNow;

        _teamRepo.Update(teamEntity);
        await _uow.SaveChangesAsync(ct);

        return inviteCode;
    }
}
