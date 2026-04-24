using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class ExtendContestTimeCommandHandler
    : IRequestHandler<ExtendContestTimeCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExtendContestTimeCommandHandler> _logger;

    public ExtendContestTimeCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<ExtendContestTimeCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(ExtendContestTimeCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        if (contest.StartAt > now)
            throw new InvalidOperationException("CONTEST_NOT_STARTED");

        if (contest.EndAt <= now)
            throw new InvalidOperationException("CONTEST_ALREADY_ENDED");

        var newEnd = DateTime.SpecifyKind(request.NewEndAt, DateTimeKind.Utc);

        if (newEnd <= contest.EndAt)
            throw new ArgumentException("NEW_END_MUST_BE_LATER");

        contest.EndAt = newEnd;
        contest.UpdatedAt = now;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contest time extended | ContestId={ContestId} | NewEnd={NewEnd} | By={UserId}",
            contest.Id, newEnd, userId);

        return true;
    }
}
