using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Application.UseCases.Contests.Commands;

public class RegisterContestCommandHandler
    : IRequestHandler<RegisterContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IUserRepository _userRepo;
    private readonly IWriteRepository<Team, Guid> _teamRepo;
    private readonly IWriteRepository<TeamMember, Guid> _memberRepo;
    private readonly IWriteRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IContestRepository _contestCustomRepo;
    private readonly ITeamRepository _teamCustomRepo;
    private readonly IContestStatusService _statusService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public RegisterContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IUserRepository userRepo,
        IWriteRepository<Team, Guid> teamRepo,
        IWriteRepository<TeamMember, Guid> memberRepo,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IContestRepository contestCustomRepo,
        ITeamRepository teamCustomRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _userRepo = userRepo;
        _teamRepo = teamRepo;
        _memberRepo = memberRepo;
        _contestTeamRepo = contestTeamRepo;
        _contestCustomRepo = contestCustomRepo;
        _teamCustomRepo = teamCustomRepo;
        _statusService = statusService;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(RegisterContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // ======================
        // USER CHECK
        // ======================
        var isActive = await _userRepo.IsUserActiveAsync(userId);
        if (!isActive)
            throw new Exception("Account is locked");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new Exception("Contest not found");

        if (contest.VisibilityCode != "public")
            throw new Exception("Contest is not public");

        if (contest.EndAt < DateTime.UtcNow)
            throw new Exception("Contest ended");

        if (!_statusService.CanRegister(contest.StartAt))
            throw new Exception("Registration closed");

        if (!contest.AllowTeams && request.IsTeam)
            throw new Exception("Contest does not allow team mode");

        Guid teamId;

        // ======================================================
        // 👤 SOLO MODE
        // ======================================================
        if (!request.IsTeam)
        {
            var conflict = await _contestCustomRepo
                .HasTimeConflictAsync(userId, contest.StartAt, contest.EndAt);

            if (conflict)
            {
                var conflictUser = await _userRepo
                    .GetUserDisplayByIdsAsync(new List<Guid> { userId });

                throw new Exception(
                    $"TIME_CONFLICT_USERS:{JsonSerializer.Serialize(conflictUser)}"
                );
            }

            var teams = await _teamCustomRepo.GetTeamsByUserAsync(userId);
            var personal = teams.FirstOrDefault(x => x.IsPersonal);

            if (personal != null)
            {
                teamId = personal.Id;
            }
            else
            {
                var team = new Team
                {
                    Id = Guid.NewGuid(),
                    LeaderId = userId,
                    TeamName = $"User-{userId}",
                    IsPersonal = true,
                    TeamSize = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _teamRepo.AddAsync(team, ct);

                await _memberRepo.AddAsync(new TeamMember
                {
                    TeamId = team.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                }, ct);

                teamId = team.Id;
            }
        }

        // ======================================================
        // 👥 TEAM MODE
        // ======================================================
        else
        {
            if (string.IsNullOrWhiteSpace(request.TeamName))
                throw new Exception("Team name required");

            // 🔥 FIX DUPLICATE + AUTO ADD LEADER
            var allMembers = new HashSet<Guid>(request.MemberIds ?? new List<Guid>())
            {
                userId
            };

            if (allMembers.Count < 3)
                throw new Exception("Team must have at least 3 members");

            if (allMembers.Count > 5)
                throw new Exception("Max 5 members");

            // CHECK LOCK USERS
            var lockedUsers = await _userRepo.GetLockedUsersAsync(allMembers.ToList());
            if (lockedUsers.Any())
                throw new Exception("Some members are locked");

            // TIME CONFLICT CHECK
            var conflictedUsers = new List<Guid>();

            foreach (var m in allMembers)
            {
                var conflict = await _contestCustomRepo
                    .HasTimeConflictAsync(m, contest.StartAt, contest.EndAt);

                if (conflict)
                    conflictedUsers.Add(m);
            }

            if (conflictedUsers.Any())
            {
                var conflictUsers = await _userRepo
                    .GetUserDisplayByIdsAsync(conflictedUsers);

                throw new Exception(
                    $"TIME_CONFLICT_USERS:{JsonSerializer.Serialize(conflictUsers)}"
                );
            }

            // CREATE TEAM
            var team = new Team
            {
                Id = Guid.NewGuid(),
                LeaderId = userId,
                TeamName = request.TeamName,
                TeamSize = allMembers.Count,
                IsPersonal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _teamRepo.AddAsync(team, ct);

            // 🔥 ADD MEMBERS (NO DUPLICATE TRACKING)
            foreach (var m in allMembers)
            {
                await _memberRepo.AddAsync(new TeamMember
                {
                    TeamId = team.Id,
                    UserId = m,
                    JoinedAt = DateTime.UtcNow
                }, ct);
            }

            teamId = team.Id;
        }

        // ======================
        // CHECK ALREADY JOINED
        // ======================
        var joined = await _contestCustomRepo
            .IsTeamJoinedAsync(request.ContestId, teamId);

        if (joined)
            throw new Exception("Already joined");

        // ======================
        // INSERT CONTEST TEAM
        // ======================
        var entry = new ContestTeam
        {
            Id = Guid.NewGuid(),
            ContestId = request.ContestId,
            TeamId = teamId,
            JoinAt = DateTime.UtcNow
        };

        await _contestTeamRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return entry.Id;
    }
}