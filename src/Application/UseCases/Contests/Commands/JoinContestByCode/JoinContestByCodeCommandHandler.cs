using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class JoinContestByCodeCommandHandler
    : IRequestHandler<JoinContestByCodeCommand, JoinByCodeResult>
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

    public async Task<JoinByCodeResult> Handle(JoinContestByCodeCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            throw new ArgumentException("INVITE_CODE_REQUIRED");

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("UNAUTHORIZED");

        var isActive = await _userRepo.IsUserActiveAsync(userId);
        if (!isActive)
            throw new ArgumentException("ACCOUNT_LOCKED");

        var code = request.InviteCode.Trim().ToUpper();

        // =========================
        // PHÂN BIỆT: 10 ký tự = contest code, 8 ký tự = team invite code
        // =========================
        if (code.Length == 8)
            return await HandleTeamCode(code, userId, ct);

        if (code.Length == 10)
            return await HandleContestCode(code, userId, ct);

        throw new ArgumentException("INVALID_INVITE_CODE");
    }

    // =========================
    // TEAM CODE FLOW (8 ký tự)
    // Join thẳng vào team, tự động vào contest nếu team đang trong contest
    // =========================
    private async Task<JoinByCodeResult> HandleTeamCode(string code, Guid userId, CancellationToken ct)
    {
        var team = await _teamCustomRepo.GetByInviteCodeAsync(code)
            ?? throw new KeyNotFoundException("INVALID_INVITE_CODE");

        var alreadyInTeam = await _teamCustomRepo.IsUserInTeamAsync(userId, team.Id);

        if (!alreadyInTeam)
        {
            var count = await _teamCustomRepo.GetTeamMemberCountAsync(team.Id);
            if (count >= 3)
                throw new InvalidOperationException("TEAM_FULL");

            await _memberRepo.AddAsync(new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            }, ct);

            team.TeamSize = count + 1;
            team.UpdatedAt = DateTime.UtcNow;
            _teamRepo.Update(team);
        }

        // Nếu team đang trong contest active → tự động thêm ContestTeam
        Guid? contestTeamId = null;
        Guid? contestId = null;

        var contest = await _contestCustomRepo.GetActiveContestByTeamIdAsync(team.Id);

        if (contest != null)
        {
            contestId = contest.Id;
            var joined = await _contestCustomRepo.IsTeamJoinedAsync(contest.Id, team.Id);

            if (joined)
            {
                var existing = await _contestCustomRepo.GetContestTeamAsync(contest.Id, team.Id);
                contestTeamId = existing?.Id;
            }
            else
            {
                var entry = new ContestTeam
                {
                    Id = Guid.NewGuid(),
                    ContestId = contest.Id,
                    TeamId = team.Id,
                    JoinAt = DateTime.UtcNow
                };
                await _contestTeamRepo.AddAsync(entry, ct);
                contestTeamId = entry.Id;
            }
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Joined by team code | TeamId={TeamId} | UserId={UserId} | ContestId={ContestId}",
            team.Id, userId, contestId);

        return new JoinByCodeResult
        {
            Type = "team",
            TeamId = team.Id,
            ContestTeamId = contestTeamId,
            ContestId = contestId
        };
    }

    // =========================
    // CONTEST CODE FLOW (10 ký tự)
    // Tạo personal team nếu cần, join contest
    // =========================
    private async Task<JoinByCodeResult> HandleContestCode(string code, Guid userId, CancellationToken ct)
    {
        var contest = await _contestRepo.FirstOrDefaultAsync(
            new ContestByInviteCodeSpec(code), ct)
            ?? throw new KeyNotFoundException("INVALID_INVITE_CODE");

        if (contest.EndAt < DateTime.UtcNow)
            throw new ArgumentException("CONTEST_ENDED");

        if (!_statusService.CanRegister(contest.StartAt))
            throw new ArgumentException("REGISTRATION_CLOSED");

        var conflict = await _contestCustomRepo
            .HasTimeConflictAsync(userId, contest.StartAt, contest.EndAt);

        if (conflict)
            throw new InvalidOperationException("TIME_CONFLICT");

        var teams = await _teamCustomRepo.GetTeamsByUserAsync(userId);
        var personal = teams.FirstOrDefault(x => x.IsPersonal);

        Guid teamId;

        if (personal != null)
        {
            teamId = personal.Id;
        }
        else
        {
            var users = await _userRepo.GetUsersByIdsAsync(new List<Guid> { userId });
            var displayName = users.FirstOrDefault()?.DisplayName ?? userId.ToString();

            var team = new Team
            {
                Id = Guid.NewGuid(),
                LeaderId = userId,
                TeamName = displayName,
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

        var joined = await _contestCustomRepo.IsTeamJoinedAsync(contest.Id, teamId);

        if (joined)
            throw new InvalidOperationException("ALREADY_JOINED");

        var entry = new ContestTeam
        {
            Id = Guid.NewGuid(),
            ContestId = contest.Id,
            TeamId = teamId,
            JoinAt = DateTime.UtcNow
        };

        await _contestTeamRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Joined by contest code | ContestId={ContestId} | TeamId={TeamId} | UserId={UserId}",
            contest.Id, teamId, userId);

        return new JoinByCodeResult
        {
            Type = "contest",
            TeamId = teamId,
            ContestTeamId = entry.Id,
            ContestId = contest.Id
        };
    }
}
