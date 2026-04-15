using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class RemixContestCommandHandler
    : IRequestHandler<RemixContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _contestWriteRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _contestProblemRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RemixContestCommandHandler> _logger;

    public RemixContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> contestWriteRepo,
        IWriteRepository<ContestProblem, Guid> contestProblemRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<RemixContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _contestWriteRepo = contestWriteRepo;
        _contestProblemRepo = contestProblemRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(RemixContestCommand request, CancellationToken ct)
    {
        // =========================
        // 1. AUTH
        // =========================
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("Unauthorized remix attempt");
            throw new UnauthorizedAccessException("UNAUTHORIZED");
        }

        var userId = _currentUser.UserId!.Value;

        _logger.LogInformation(
            "RemixContest START | User={UserId} | SourceContest={ContestId}",
            userId, request.SourceContestId);

        // =========================
        // 2. VALIDATION
        // =========================
        if (request.StartAt.HasValue && request.EndAt.HasValue &&
            request.StartAt >= request.EndAt)
        {
            throw new ArgumentException("INVALID_TIME_RANGE");
        }

        // =========================
        // 3. LOAD SOURCE
        // =========================
        var source = await _contestRepo.FirstOrDefaultAsync(
            new RemixContestSpec(request.SourceContestId),
            ct);

        if (source == null)
        {
            _logger.LogWarning("Source contest not found: {ContestId}", request.SourceContestId);
            throw new Exception("CONTEST_NOT_FOUND");
        }

        // =========================
        // 4. PERMISSION
        // =========================
        var isOwner = source.CreatedBy == userId;
        var isAdmin =
            _currentUser.IsInRole("admin") ||
            _currentUser.IsInRole("manager");

        if (!isOwner && !isAdmin)
        {
            _logger.LogWarning(
                "Permission denied | User={UserId} | Contest={ContestId}",
                userId, source.Id);

            throw new UnauthorizedAccessException("NO_PERMISSION");
        }

        // =========================
        // 5. CREATE CONTEST
        // =========================
        var now = DateTime.UtcNow;
        var newContestId = Guid.NewGuid();

        var newContest = new Contest
        {
            Id = newContestId,

            Title = string.IsNullOrWhiteSpace(request.Title)
                ? $"{source.Title} (Remix)"
                : request.Title,

            DescriptionMd = source.DescriptionMd,

            VisibilityCode = request.VisibilityCode ?? "private",
            ContestType = source.ContestType,
            AllowTeams = source.AllowTeams,

            StartAt = request.StartAt ?? now,
            EndAt = request.EndAt ?? now.AddHours(3),

            RemixOfContestId = source.Id,
            IsVirtual = request.IsVirtual,

            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId,

            IsActive = true
        };

        // =========================
        // 6. CLONE PROBLEMS (100% FIELD EXISTING ONLY)
        // =========================
        var sourceProblems = source.ContestProblems ?? new List<ContestProblem>();

        var clonedProblems = sourceProblems
            .Where(cp => cp.IsActive)
            .Select(cp => new ContestProblem
            {
                Id = Guid.NewGuid(),
                ContestId = newContestId,
                ProblemId = cp.ProblemId,

                Ordinal = cp.Ordinal,
                Alias = cp.Alias,
                DisplayIndex = cp.DisplayIndex,

                Points = cp.Points,
                TimeLimitMs = cp.TimeLimitMs,
                MemoryLimitKb = cp.MemoryLimitKb,

                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            })
            .ToList();

        _logger.LogInformation("Cloned {Count} problems", clonedProblems.Count);

        // =========================
        // 7. SAVE (EF transaction nội bộ)
        // =========================
        try
        {
            await _contestWriteRepo.AddAsync(newContest, ct);

            if (clonedProblems.Count > 0)
            {
                await _contestProblemRepo.AddRangeAsync(clonedProblems, ct);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "RemixContest SUCCESS | NewContestId={NewId}",
                newContestId);

            return newContestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RemixContest FAILED | User={UserId} | SourceContest={ContestId}",
                userId, request.SourceContestId);

            throw;
        }
    }
}