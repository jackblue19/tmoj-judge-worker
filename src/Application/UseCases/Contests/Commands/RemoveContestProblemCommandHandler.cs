using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class RemoveContestProblemCommandHandler
    : IRequestHandler<RemoveContestProblemCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpReadRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _cpWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RemoveContestProblemCommandHandler> _logger;

    public RemoveContestProblemCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpReadRepo,
        IWriteRepository<ContestProblem, Guid> cpWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<RemoveContestProblemCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _cpReadRepo = cpReadRepo;
        _cpWriteRepo = cpWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveContestProblemCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        if (contest.StartAt <= now)
            throw new InvalidOperationException("CONTEST_ALREADY_STARTED");

        var cp = await _cpReadRepo.GetByIdAsync(request.ContestProblemId, ct)
            ?? throw new KeyNotFoundException("CONTEST_PROBLEM_NOT_FOUND");

        if (cp.ContestId != request.ContestId)
            throw new KeyNotFoundException("CONTEST_PROBLEM_NOT_FOUND");

        _cpWriteRepo.Remove(cp);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Contest problem removed | ContestId={ContestId} | CpId={CpId} | By={UserId}",
            contest.Id, cp.Id, userId);

        return true;
    }
}
