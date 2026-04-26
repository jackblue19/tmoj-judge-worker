using Application.Common.Interfaces;
using Application.UseCases.Teams.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class AddTeamMemberCommandHandler
    : IRequestHandler<AddTeamMemberCommand, Guid>
{
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly IReadRepository<TeamMember, Guid> _memberReadRepo;
    private readonly IWriteRepository<TeamMember, Guid> _memberWriteRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly ITeamRepository _teamCustomRepo;
    private readonly IContestRepository _contestRepo;
    private readonly IContestStatusService _contestStatus;
    private readonly IUserRepository _userRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _uow;

    public AddTeamMemberCommandHandler(
        IReadRepository<Team, Guid> teamRepo,
        IReadRepository<TeamMember, Guid> memberReadRepo,
        IWriteRepository<TeamMember, Guid> memberWriteRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        ITeamRepository teamCustomRepo,
        IContestRepository contestRepo,
        IContestStatusService contestStatus,
        IUserRepository userRepo,
        ICurrentUserService currentUser,
        INotificationRepository notificationRepo,
        IUnitOfWork uow)
    {
        _teamRepo = teamRepo;
        _memberReadRepo = memberReadRepo;
        _memberWriteRepo = memberWriteRepo;
        _teamWriteRepo = teamWriteRepo;
        _teamCustomRepo = teamCustomRepo;
        _contestRepo = contestRepo;
        _contestStatus = contestStatus;
        _userRepo = userRepo;
        _currentUser = currentUser;
        _notificationRepo = notificationRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddTeamMemberCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // ======================
        // 1. CHECK TEAM
        // ======================
        var team = await _teamRepo.GetByIdAsync(request.TeamId, ct);
        if (team == null)
            throw new Exception("Team not found");

        // ======================
        // 2. CHECK LEADER
        // ======================
        var isLeader = await _teamCustomRepo
            .IsUserLeaderAsync(currentUserId, request.TeamId);

        if (!isLeader)
            throw new Exception("Only leader can add members");

        // ======================
        // 🔥 3. CHECK TIME WINDOW (CLEAN)
        // ======================
        var contest = await _contestRepo
            .GetActiveContestByTeamIdAsync(team.Id);

        if (contest != null)
        {
            var canEdit = _contestStatus
                .CanUnregister(contest.StartAt);

            if (!canEdit)
                throw new Exception(
                    "Cannot modify team after unregister deadline (4h before contest)");
        }

        // ======================
        // 4. CHECK USER LOCK
        // ======================
        var isActive = await _userRepo.IsUserActiveAsync(request.UserId);
        if (!isActive)
            throw new Exception("User is locked");

        // ======================
        // 5. CHECK USER ALREADY IN TEAM
        // ======================
        var isInTeam = await _teamCustomRepo
            .IsUserInTeamAsync(request.UserId, request.TeamId);

        if (isInTeam)
            throw new Exception("User already in this team");

        // ======================
        // 6. CHECK TEAM SIZE
        // ======================
        var members = await _memberReadRepo.ListAsync(
            new TeamMemberByTeamSpec(request.TeamId), ct);

        if (members.Count >= 5)
            throw new Exception("Team max size is 5");

        // ======================
        // 7. ADD MEMBER
        // ======================
        var entity = new TeamMember
        {
            TeamId = request.TeamId,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow
        };

        await _memberWriteRepo.AddAsync(entity, ct);

        // ======================
        // 8. UPDATE TEAM SIZE
        // ======================
        team.TeamSize = members.Count + 1;
        team.UpdatedAt = DateTime.UtcNow;

        _teamWriteRepo.Update(team);

        await _uow.SaveChangesAsync(ct);

        // ======================
        // 9. SEND NOTIFICATION
        // ======================
        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Title = "Bạn đã được thêm vào nhóm",
            Message = $"Bạn đã được thêm vào nhóm '{team.TeamName}'",
            Type = "system",
            ScopeType = "team",
            ScopeId = team.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        }, ct);

        await _notificationRepo.SaveChangesAsync(ct);

        return request.UserId;
    }
}