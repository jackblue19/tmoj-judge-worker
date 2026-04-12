using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class DeleteTeamCommandHandler
    : IRequestHandler<DeleteTeamCommand, bool>
{
    private readonly IReadRepository<Team, Guid> _readRepo;
    private readonly IWriteRepository<Team, Guid> _writeRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public DeleteTeamCommandHandler(
        IReadRepository<Team, Guid> readRepo,
        IWriteRepository<Team, Guid> writeRepo,
        ITeamRepository teamRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTeamCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // =========================
        // GET TEAM
        // =========================
        var team = await _readRepo.GetByIdAsync(request.TeamId, ct);

        if (team == null)
            throw new Exception("Team not found");

        // =========================
        // CHECK PERMISSION
        // =========================
        var isLeader = await _teamRepo
            .IsUserLeaderAsync(userId.Value, request.TeamId);

        var isAdmin = _currentUser.IsInRole("admin")
                   || _currentUser.IsInRole("manager");

        if (!isLeader && !isAdmin)
            throw new UnauthorizedAccessException("Only leader can delete team");

        // =========================
        // DELETE TEAM
        // =========================
        _writeRepo.Remove(team);

        await _uow.SaveChangesAsync(ct);

        return true;
    }
}