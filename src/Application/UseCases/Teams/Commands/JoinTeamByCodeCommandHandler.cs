using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class JoinTeamByCodeCommandHandler
    : IRequestHandler<JoinTeamByCodeCommand>
{
    private readonly ITeamRepository _repo;
    private readonly IWriteRepository<TeamMember, Guid> _writeRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly IContestRepository _contestRepo; // 🔥 ADD
    private readonly IContestStatusService _contestStatus; // 🔥 ADD
    private readonly IUserRepository _userRepo; // 🔥 ADD
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _uow;

    public JoinTeamByCodeCommandHandler(
        ITeamRepository repo,
        IWriteRepository<TeamMember, Guid> writeRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        IContestRepository contestRepo, // 🔥 ADD
        IContestStatusService contestStatus, // 🔥 ADD
        IUserRepository userRepo, // 🔥 ADD
        ICurrentUserService currentUser,
        INotificationRepository notificationRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _writeRepo = writeRepo;
        _teamWriteRepo = teamWriteRepo;
        _contestRepo = contestRepo; // 🔥 ADD
        _contestStatus = contestStatus; // 🔥 ADD
        _userRepo = userRepo; // 🔥 ADD
        _currentUser = currentUser;
        _notificationRepo = notificationRepo;
        _uow = uow;
    }

    public async Task Handle(JoinTeamByCodeCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // =========================
        // 1. CHECK TEAM EXIST
        // =========================
        var team = await _repo.GetByInviteCodeAsync(request.Code);
        if (team == null)
            throw new Exception("Invalid code");

        // =========================
        // 🔥 2. CHECK USER ACTIVE
        // =========================
        var isActive = await _userRepo.IsUserActiveAsync(userId);
        if (!isActive)
            throw new Exception("User is locked");

        // =========================
        // 3. CHECK USER ALREADY IN TEAM
        // =========================
        var isAlreadyInTeam = await _repo
            .IsUserInTeamAsync(userId, team.Id);

        if (isAlreadyInTeam)
            throw new Exception("User already in this team");

        // ❌ REMOVE RULE SAI (KHÔNG CHẶN USER 1 TEAM)
        // var teams = await _repo.GetTeamsByUserAsync(userId);
        // if (teams.Any(x => !x.IsPersonal))
        //     throw new Exception("User already in another team");

        // =========================
        // 4. CHECK TEAM SIZE
        // =========================
        var count = await _repo.GetTeamMemberCountAsync(team.Id);

        if (count >= 3)
            throw new InvalidOperationException("TEAM_FULL");

        // =========================
        // 🔥 5. CHECK CONTEST WINDOW
        // =========================
        var contest = await _contestRepo
            .GetActiveContestByTeamIdAsync(team.Id);

        if (contest != null)
        {
            var canEdit = _contestStatus
                .CanUnregister(contest.StartAt);

            if (!canEdit)
                throw new Exception(
                    "Cannot join team after unregister deadline (4h before contest)");
        }

        // =========================
        // 6. ADD MEMBER
        // =========================
        var entity = new TeamMember
        {
            TeamId = team.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        await _writeRepo.AddAsync(entity, ct);

        // =========================
        // 7. UPDATE TEAM SIZE
        // =========================
        team.TeamSize = count + 1;
        team.UpdatedAt = DateTime.UtcNow;

        _teamWriteRepo.Update(team);

        await _uow.SaveChangesAsync(ct);

        // =========================
        // 8. NOTIFY LEADER
        // =========================
        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = team.LeaderId,
            Title = "Thành viên mới gia nhập",
            Message = $"Có thành viên mới đã gia nhập nhóm '{team.TeamName}' qua mã mời.",
            Type = "system",
            ScopeType = "team",
            ScopeId = team.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        }, ct);

        await _notificationRepo.SaveChangesAsync(ct);
    }
}