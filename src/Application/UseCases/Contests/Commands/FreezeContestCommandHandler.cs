using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class FreezeContestCommandHandler
    : IRequestHandler<FreezeContestCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FreezeContestCommandHandler> _logger;

    public FreezeContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<FreezeContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(FreezeContestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var now = DateTime.UtcNow;

        // chỉ cần contest đã bắt đầu
        if (contest.StartAt > now)
            throw new Exception("CONTEST_NOT_STARTED");

        if (contest.FreezeAt != null)
            throw new Exception("ALREADY_FROZEN");

        contest.FreezeAt = now;
        contest.UpdatedAt = now;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contest frozen | ContestId={ContestId} | By={UserId}",
            contest.Id, userId);

        return true;
    }
}