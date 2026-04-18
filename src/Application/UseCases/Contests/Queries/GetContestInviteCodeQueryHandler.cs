using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestInviteCodeQueryHandler
    : IRequestHandler<GetContestInviteCodeQuery, string?>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly ICurrentUserService _currentUser;

    public GetContestInviteCodeQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _currentUser = currentUser;
    }

    public async Task<string?> Handle(GetContestInviteCodeQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");
        var isOwner = contest.CreatedBy == userId;

        if (!isAdmin && !isOwner)
            throw new UnauthorizedAccessException("NO_PERMISSION");

        return contest.InviteCode;
    }
}
