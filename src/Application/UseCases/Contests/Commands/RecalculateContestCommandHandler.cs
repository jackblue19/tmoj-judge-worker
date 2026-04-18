using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class RecalculateContestCommandHandler
    : IRequestHandler<RecalculateContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ScoreRecalcJob, Guid> _jobReadRepo;
    private readonly IWriteRepository<ScoreRecalcJob, Guid> _jobWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RecalculateContestCommandHandler> _logger;

    public RecalculateContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ScoreRecalcJob, Guid> jobReadRepo,
        IWriteRepository<ScoreRecalcJob, Guid> jobWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<RecalculateContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _jobReadRepo = jobReadRepo;
        _jobWriteRepo = jobWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(RecalculateContestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var pendingJobs = await _jobReadRepo.ListAsync(
            new Specs.PendingRecalcJobByContestSpec(request.ContestId), ct);

        if (pendingJobs.Any())
            throw new Exception("RECALC_ALREADY_PENDING");

        var now = DateTime.UtcNow;
        var jobId = Guid.NewGuid();

        var job = new ScoreRecalcJob
        {
            Id = jobId,
            ContestId = contest.Id,
            Scope = "contest",
            Status = "pending",
            EnqueueAt = now
        };

        await _jobWriteRepo.AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recalculate enqueued | ContestId={ContestId} | JobId={JobId} | By={UserId}",
            contest.Id, jobId, userId);

        return jobId;
    }
}
