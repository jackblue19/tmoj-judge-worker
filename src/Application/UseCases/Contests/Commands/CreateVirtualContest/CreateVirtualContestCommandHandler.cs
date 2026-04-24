using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class CreateVirtualContestCommandHandler
    : IRequestHandler<CreateVirtualContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _contestWriteRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _contestProblemRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateVirtualContestCommandHandler> _logger;

    public CreateVirtualContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> contestWriteRepo,
        IWriteRepository<ContestProblem, Guid> contestProblemRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<CreateVirtualContestCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _contestWriteRepo = contestWriteRepo;
        _contestProblemRepo = contestProblemRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateVirtualContestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        _logger.LogInformation(
            "CreateVirtualContest START | User={UserId} | Source={ContestId}",
            userId, request.SourceContestId);

        var source = await _contestRepo.FirstOrDefaultAsync(
            new RemixContestSpec(request.SourceContestId), ct);

        if (source == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        if (source.EndAt > now)
            throw new Exception("CONTEST_NOT_ENDED");

        var duration = source.EndAt - source.StartAt;
        var newContestId = Guid.NewGuid();

        var newContest = new Contest
        {
            Id = newContestId,
            Title = $"{source.Title} (Virtual)",
            DescriptionMd = source.DescriptionMd,
            VisibilityCode = "private",
            ContestType = source.ContestType,
            AllowTeams = source.AllowTeams,
            StartAt = now,
            EndAt = now.Add(duration),
            RemixOfContestId = source.Id,
            IsVirtual = true,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId
        };

        var clonedProblems = (source.ContestProblems ?? new List<ContestProblem>())
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

        _logger.LogInformation("Cloned {Count} problems for virtual contest", clonedProblems.Count);

        try
        {
            await _contestWriteRepo.AddAsync(newContest, ct);

            if (clonedProblems.Count > 0)
                await _contestProblemRepo.AddRangeAsync(clonedProblems, ct);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CreateVirtualContest SUCCESS | NewContestId={NewId}",
                newContestId);

            return newContestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CreateVirtualContest FAILED | User={UserId} | Source={ContestId}",
                userId, request.SourceContestId);
            throw;
        }
    }
}
