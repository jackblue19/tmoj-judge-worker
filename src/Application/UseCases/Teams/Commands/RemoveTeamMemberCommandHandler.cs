using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class RemoveTeamMemberCommandHandler
    : IRequestHandler<RemoveTeamMemberCommand>
{
    private readonly ITeamRepository _teamRepo;
    private readonly IContestRepository _contestRepo;
    private readonly IContestStatusService _statusService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public RemoveTeamMemberCommandHandler(
        ITeamRepository teamRepo,
        IContestRepository contestRepo,
        IContestStatusService statusService,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _teamRepo = teamRepo;
        _contestRepo = contestRepo;
        _statusService = statusService;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // ======================
        // 1. CHECK LEADER
        // ======================
        var isLeader = await _teamRepo
            .IsUserLeaderAsync(userId.Value, request.TeamId);

        if (!isLeader)
            throw new Exception("Only leader can remove members");

        // ======================
        // 🔥 2. CHECK TIME WINDOW
        // ======================
        var contest = await _contestRepo
            .GetActiveContestByTeamIdAsync(request.TeamId);

        if (contest != null)
        {
            var canEdit = _statusService
                .CanUnregister(contest.StartAt);

            if (!canEdit)
                throw new Exception(
                    "Cannot modify team after unregister deadline (4h before contest)");
        }

        // ======================
        // 3. FIND MEMBER
        // ======================
        var member = await _teamRepo
            .GetTeamMemberAsync(request.TeamId, request.UserId);

        if (member == null)
            throw new Exception("Member not found");

        // ❌ không cho leader tự kick chính mình (optional nhưng nên có)
        if (member.UserId == userId.Value)
            throw new Exception("Leader cannot remove themselves");

        // ======================
        // 4. DELETE
        // ======================
        _teamRepo.DeleteTeamMember(member);

        await _uow.SaveChangesAsync(ct);
    }
}