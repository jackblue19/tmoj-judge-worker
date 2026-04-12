using Application.Common.Interfaces;
using Domain.Abstractions;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class RemoveTeamMemberCommandHandler
    : IRequestHandler<RemoveTeamMemberCommand>
{
    private readonly ITeamRepository _teamRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public RemoveTeamMemberCommandHandler(
        ITeamRepository teamRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _teamRepo = teamRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // chỉ leader được remove
        var isLeader = await _teamRepo
            .IsUserLeaderAsync(userId.Value, request.TeamId);

        if (!isLeader)
            throw new Exception("Only leader can remove members");

        var member = await _teamRepo
            .GetTeamMemberAsync(request.TeamId, request.UserId);

        if (member == null)
            throw new Exception("Member not found");

        _teamRepo.DeleteTeamMember(member);

        await _uow.SaveChangesAsync(ct);
    }
}