using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class UnfreezeContestCommandHandler
    : IRequestHandler<UnfreezeContestCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UnfreezeContestCommandHandler> _logger;

    public UnfreezeContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<UnfreezeContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(UnfreezeContestCommand request, CancellationToken ct)
    {
        // =========================
        // AUTH
        // =========================
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        // =========================
        // GET CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        // =========================
        // VALIDATE: must be frozen
        // =========================
        if (!contest.FreezeAt.HasValue || now < contest.FreezeAt.Value)
            throw new Exception("NOT_FROZEN");

        if (contest.UnfreezeAt.HasValue && now >= contest.UnfreezeAt.Value)
            throw new Exception("ALREADY_UNFROZEN");

        // Đánh dấu mốc unfreeze — FreezeContestPatch.IsFrozen sẽ dùng cả 2 cờ.
        contest.UnfreezeAt = now;

        contest.UpdatedAt = now;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contest unfrozen | ContestId={ContestId} | By={UserId}",
            contest.Id, userId);

        return true;
    }
}