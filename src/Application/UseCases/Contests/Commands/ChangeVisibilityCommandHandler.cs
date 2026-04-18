using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class ChangeVisibilityCommandHandler
    : IRequestHandler<ChangeVisibilityCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChangeVisibilityCommandHandler> _logger;

    public ChangeVisibilityCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<ChangeVisibilityCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(ChangeVisibilityCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var newVisibility = request.VisibilityCode.ToLower();
        var valid = new[] { "public", "private", "hidden" };

        if (!valid.Contains(newVisibility))
            throw new Exception("INVALID_VISIBILITY");

        var now = DateTime.UtcNow;
        var currentVisibility = contest.VisibilityCode?.ToLower() ?? "private";

        if (contest.StartAt <= now &&
            currentVisibility == "hidden" &&
            newVisibility == "public")
        {
            throw new Exception("CANNOT_PUBLIC_WHILE_RUNNING");
        }

        contest.VisibilityCode = newVisibility;

        if (newVisibility == "private" && string.IsNullOrEmpty(contest.InviteCode))
        {
            contest.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        contest.UpdatedAt = now;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Visibility changed | ContestId={ContestId} | From={From} To={To} | By={UserId}",
            contest.Id, currentVisibility, newVisibility, userId);

        return true;
    }
}
