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
            throw new ArgumentException("ACCOUNT_LOCKED");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        if (contest.VisibilityCode != "public")
            throw new ArgumentException("CONTEST_NOT_PUBLIC");

        if (contest.EndAt < DateTime.UtcNow)
            throw new ArgumentException("CONTEST_ENDED");

        if (!_statusService.CanRegister(contest.StartAt))
            throw new ArgumentException("REGISTRATION_CLOSED");

        if (!contest.AllowTeams && request.IsTeam)
            throw new ArgumentException("TEAM_MODE_NOT_ALLOWED");

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

                throw new InvalidOperationException(
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
                throw new ArgumentException("TEAM_NAME_REQUIRED");

            // 🔥 FIX DUPLICATE + AUTO ADD LEADER
            var allMembers = new HashSet<Guid>(request.MemberIds ?? new List<Guid>())
            {
                userId
            };

            if (allMembers.Count > 3)
                throw new ArgumentException("TEAM_MAX_3_MEMBERS");

            // CHECK ALL MEMBERS EXIST
            var memberList = allMembers.ToList();
            var foundMembers = await _userRepo.GetUserDisplayByIdsAsync(memberList);
            if (foundMembers.Count != memberList.Count)
            {
                var foundIds = foundMembers.Select(u => u.UserId).ToHashSet();
                var missing = memberList.Where(id => !foundIds.Contains(id)).ToList();
                throw new ArgumentException(
                    $"MEMBERS_NOT_FOUND:{JsonSerializer.Serialize(missing)}");
            }

            // CHECK LOCK USERS
            var lockedUsers = await _userRepo.GetLockedUsersAsync(memberList);
            if (lockedUsers.Any())
                throw new ArgumentException("SOME_MEMBERS_LOCKED");

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

                throw new InvalidOperationException(
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
                InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
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
            throw new InvalidOperationException("ALREADY_JOINED");

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