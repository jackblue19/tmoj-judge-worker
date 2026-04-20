using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class JoinContestByCodeCommandHandler
    : IRequestHandler<JoinContestByCodeCommand, Guid>
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
    private readonly ILogger<JoinContestByCodeCommandHandler> _logger;

    public JoinContestByCodeCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IUserRepository userRepo,
        IWriteRepository<Team, Guid> teamRepo,
        IWriteRepository<TeamMember, Guid> memberRepo,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IContestRepository contestCustomRepo,
        ITeamRepository teamCustomRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<JoinContestByCodeCommandHandler> logger)
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
        _logger = logger;
    }

    public async Task<Guid> Handle(JoinContestByCodeCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            throw new ArgumentException("INVITE_CODE_REQUIRED");

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("UNAUTHORIZED");

        var isActive = await _userRepo.IsUserActiveAsync(userId);
        if (!isActive)
            throw new ArgumentException("ACCOUNT_LOCKED");

        var code = request.InviteCode.Trim();

        // 1. Thử tìm contest theo code
        var contest = await _contestRepo.FirstOrDefaultAsync(
            new ContestByInviteCodeSpec(code), ct);

        Team? teamFromCode = null;

        if (contest == null)
        {
            // 2. Không phải contest code → thử team code
            teamFromCode = await _teamCustomRepo.GetByInviteCodeAsync(code);

            if (teamFromCode == null)
                throw new KeyNotFoundException("INVALID_INVITE_CODE");

            contest = await _contestCustomRepo
                .GetActiveContestByTeamIdAsync(teamFromCode.Id)
                ?? throw new KeyNotFoundException("TEAM_NOT_IN_CONTEST");
        }

        if (contest.EndAt < DateTime.UtcNow)
            throw new ArgumentException("CONTEST_ENDED");

        if (!_statusService.CanRegister(contest.StartAt))
            throw new ArgumentException("REGISTRATION_CLOSED");

        var conflict = await _contestCustomRepo
            .HasTimeConflictAsync(userId, contest.StartAt, contest.EndAt);

        if (conflict)
            throw new InvalidOperationException("TIME_CONFLICT");

        Guid teamId;

        // =========================
        // TEAM CODE FLOW — join thẳng team đó (user vào team + thành participant contest)
        // =========================
        if (teamFromCode != null)
        {
            var alreadyInTeam = await _teamCustomRepo
                .IsUserInTeamAsync(userId, teamFromCode.Id);

            if (!alreadyInTeam)
            {
                var count = await _teamCustomRepo.GetTeamMemberCountAsync(teamFromCode.Id);
                if (count >= 3)
                    throw new InvalidOperationException("TEAM_FULL");

                await _memberRepo.AddAsync(new TeamMember
                {
                    TeamId = teamFromCode.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                }, ct);

                teamFromCode.TeamSize = count + 1;
                teamFromCode.UpdatedAt = DateTime.UtcNow;
                _teamRepo.Update(teamFromCode);
            }

            teamId = teamFromCode.Id;
        }
        else
        {
            // =========================
            // CONTEST CODE FLOW — join qua personal team
            // =========================
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

        var joined = await _contestCustomRepo.IsTeamJoinedAsync(contest.Id, teamId);

        Guid contestTeamId;

        if (joined)
        {
            // Team code flow — team đã ở contest, user vừa vào team thì coi như xong.
            if (teamFromCode == null)
                throw new InvalidOperationException("ALREADY_JOINED");

            var existingEntry = await _contestCustomRepo
                .GetContestTeamAsync(contest.Id, teamId)
                ?? throw new InvalidOperationException("CONTEST_TEAM_NOT_FOUND");

            contestTeamId = existingEntry.Id;
        }
        else
        {
            var entry = new ContestTeam
            {
                Id = Guid.NewGuid(),
                ContestId = contest.Id,
                TeamId = teamId,
                JoinAt = DateTime.UtcNow
            };

            await _contestTeamRepo.AddAsync(entry, ct);
            contestTeamId = entry.Id;
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Joined by code | ContestId={ContestId} | TeamId={TeamId} | User={UserId} | ByTeamCode={ByTeam}",
            contest.Id, teamId, userId, teamFromCode != null);

        return contestTeamId;
    }
}
