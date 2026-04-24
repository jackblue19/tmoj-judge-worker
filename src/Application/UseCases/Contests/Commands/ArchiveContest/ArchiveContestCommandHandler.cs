using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class ArchiveContestCommandHandler
    : IRequestHandler<ArchiveContestCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArchiveContestCommandHandler> _logger;

    public ArchiveContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<ArchiveContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(ArchiveContestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        if (contest.VisibilityCode != "private")
            throw new InvalidOperationException("ONLY_PRIVATE_CAN_BE_ARCHIVED");

        if (contest.EndAt > now)
            throw new InvalidOperationException("CONTEST_NOT_ENDED");

        if (!contest.IsActive)
            throw new InvalidOperationException("ALREADY_ARCHIVED");

        contest.IsActive = false;
        contest.UpdatedAt = now;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contest archived | ContestId={ContestId} | By={UserId}",
            contest.Id, userId);

        return true;
    }
}
