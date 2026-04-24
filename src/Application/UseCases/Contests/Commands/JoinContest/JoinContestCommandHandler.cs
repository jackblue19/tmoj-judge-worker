using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestCommandHandler
    : IRequestHandler<JoinContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly IUserRepository _userRepo;
    private readonly IWriteRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IContestRepository _contestCustomRepo;
    private readonly ITeamRepository _teamCustomRepo;
    private readonly IContestStatusService _statusService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public JoinContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<Team, Guid> teamRepo,
        IUserRepository userRepo,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IContestRepository contestCustomRepo,
        ITeamRepository teamCustomRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _teamRepo = teamRepo;
        _userRepo = userRepo;
        _contestTeamRepo = contestTeamRepo;
        _contestCustomRepo = contestCustomRepo;
        _teamCustomRepo = teamCustomRepo;
        _statusService = statusService;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(JoinContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // =========================
        // 🔥 CHECK USER LOCK
        // =========================
        var isActive = await _userRepo.IsUserActiveAsync(userId);
        if (!isActive)
            throw new Exception("Account is locked");

        // =========================
        // CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new Exception("Contest not found");

        if (contest.EndAt < DateTime.UtcNow)
            throw new Exception("Contest ended");

        // 🔥 NEW: CHECK JOIN TIME (PHẢI ĐANG TRONG CONTEST)
        if (!_statusService.CanJoin(contest.StartAt, contest.EndAt))
            throw new Exception("Cannot join outside contest time");

        // 🔥 CHECK ALLOW TEAM
        if (!contest.AllowTeams)
            throw new Exception("This contest does not allow teams");

        // =========================
        // TEAM
        // =========================
        var team = await _teamRepo.GetByIdAsync(request.TeamId, ct)
            ?? throw new Exception("Team not found");

        var isMember = await _teamCustomRepo
            .IsUserInTeamAsync(userId, team.Id);

        if (!isMember)
            throw new Exception("Not in team");

        var isValid = await _teamCustomRepo
            .IsTeamValidForContestAsync(team.Id);

        if (!isValid)
            throw new ArgumentException("TEAM_MAX_3_MEMBERS");

        // =========================
        // MEMBER IDS
        // =========================
        var memberIds = await _contestCustomRepo
            .GetTeamMemberIdsAsync(team.Id);

        // 🔥 CHECK LOCK BATCH
        var lockedUsers = await _userRepo.GetLockedUsersAsync(memberIds);
        if (lockedUsers.Any())
            throw new Exception("Some members are locked");

        // 🔥 TIME CONFLICT
        foreach (var m in memberIds)
        {
            var conflict = await _contestCustomRepo
                .HasTimeConflictAsync(m, contest.StartAt, contest.EndAt);

            if (conflict)
                throw new Exception("Member has time conflict");
        }

        // =========================
        // ALREADY JOINED
        // =========================
        var joined = await _contestCustomRepo
            .IsTeamJoinedAsync(request.ContestId, team.Id);

        if (joined)
            throw new Exception("Already joined");

        // =========================
        // INSERT
        // =========================
        var entry = new ContestTeam
        {
            Id = Guid.NewGuid(),
            ContestId = request.ContestId,
            TeamId = team.Id,
            JoinAt = DateTime.UtcNow,
            Score = 0,
            SolvedProblem = 0,
            SubmissionsCount = 0,
            Penalty = 0
        };

        await _contestTeamRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return entry.Id;
    }
}