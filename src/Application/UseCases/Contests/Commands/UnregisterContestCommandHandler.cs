using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class UnregisterContestCommandHandler
    : IRequestHandler<UnregisterContestCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IContestRepository _contestCustomRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IContestStatusService _statusService;
    private readonly ICurrentUserService _currentUser;
    private readonly IWriteRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IUnitOfWork _uow;

    public UnregisterContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IContestRepository contestCustomRepo,
        ITeamRepository teamRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _contestCustomRepo = contestCustomRepo;
        _teamRepo = teamRepo;
        _statusService = statusService;
        _currentUser = currentUser;
        _contestTeamRepo = contestTeamRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(UnregisterContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // ======================
        // 1. CONTEST
        // ======================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new Exception("Contest not found");

        // ======================
        // 2. TIME RULE
        // ======================
        if (!_statusService.CanUnregister(contest.StartAt))
            throw new Exception("Cannot unregister within 4 hours before start");

        // ======================
        // 3. GET USER TEAM
        // ======================
        var teams = await _teamRepo.GetTeamsByUserAsync(userId);

        if (teams == null || teams.Count == 0)
            throw new Exception("User is not in any team");

        ContestTeam? target = null;

        // ======================
        // 4. FIND + LOAD REAL ENTITY VIA CONTEXT SAFE WAY
        // ======================
        foreach (var team in teams)
        {
            var joined = await _contestCustomRepo
                .IsTeamJoinedAsync(request.ContestId, team.Id);

            if (joined)
            {
                // 🔥 FIX: KHÔNG DÙNG Query()
                // dùng pattern: lấy entity bằng composite key thông qua repository custom

                target = await _contestCustomRepo
                    .GetContestTeamAsync(request.ContestId, team.Id);

                break;
            }
        }

        if (target == null)
            throw new Exception("You have not registered for this contest");

        // ======================
        // 5. REMOVE SAFE
        // ======================
        _contestTeamRepo.Remove(target);

        await _uow.SaveChangesAsync(ct);

        return true;
    }
}