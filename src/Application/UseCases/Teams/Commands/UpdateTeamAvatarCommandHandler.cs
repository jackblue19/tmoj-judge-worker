using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class UpdateTeamAvatarCommandHandler : IRequestHandler<UpdateTeamAvatarCommand>
{
    private readonly IReadRepository<Team, Guid> _teamReadRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly ITeamRepository _teamCustomRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UpdateTeamAvatarCommandHandler(
        IReadRepository<Team, Guid> teamReadRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        ITeamRepository teamCustomRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _teamReadRepo = teamReadRepo;
        _teamWriteRepo = teamWriteRepo;
        _teamCustomRepo = teamCustomRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(UpdateTeamAvatarCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("UNAUTHORIZED");

        var isLeader = await _teamCustomRepo.IsUserLeaderAsync(userId, request.TeamId);
        if (!isLeader)
            throw new UnauthorizedAccessException("ONLY_LEADER_CAN_UPDATE");

        var team = await _teamReadRepo.GetByIdAsync(request.TeamId, ct)
            ?? throw new KeyNotFoundException("TEAM_NOT_FOUND");

        team.AvatarUrl = request.AvatarUrl;
        team.UpdatedAt = DateTime.UtcNow;
        _teamWriteRepo.Update(team);

        await _uow.SaveChangesAsync(ct);
    }
}
