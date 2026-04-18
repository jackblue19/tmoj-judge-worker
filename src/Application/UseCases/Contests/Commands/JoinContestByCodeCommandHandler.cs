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

        var contest = await _contestRepo.FirstOrDefaultAsync(
            new ContestByInviteCodeSpec(request.InviteCode), ct)
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
            "Joined contest by code | ContestId={ContestId} | TeamId={TeamId} | User={UserId}",
            contest.Id, teamId, userId);

        return entry.Id;
    }
}
