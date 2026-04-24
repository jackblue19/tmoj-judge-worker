using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.UseCases.Contests.Dtos;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Commands;

public class PublishContestCommandHandler
    : IRequestHandler<PublishContestCommand, PublishContestResultDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public PublishContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _cpRepo = cpRepo;
        _writeRepo = writeRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<PublishContestResultDto> Handle(
        PublishContestCommand request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException("USER_NOT_AUTHENTICATED");

        // ======================
        // 1. GET CONTEST
        // ======================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        // ======================
        // 2. CHECK STATE
        // ======================
        if (contest.VisibilityCode == "public")
            throw new InvalidOperationException("CONTEST_ALREADY_PUBLISHED");

        // ======================
        // 3. CHECK TIME
        // ======================
        if (contest.StartAt <= now)
            throw new InvalidOperationException("CONTEST_ALREADY_STARTED");

        // ======================
        // 4. CHECK HAS PROBLEMS
        // ======================
        var problems = await _cpRepo.ListAsync(
            new ContestProblemByContestSpec(contest.Id), ct);

        if (!problems.Any())
            throw new InvalidOperationException("CONTEST_HAS_NO_PROBLEMS");

        // ======================
        // 5. PUBLISH
        // ======================
        contest.VisibilityCode = "public";

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        // ======================
        // 6. RESPONSE
        // ======================
        return new PublishContestResultDto
        {
            ContestId = contest.Id,
            Title = contest.Title,
            Status = "published",
            Visibility = contest.VisibilityCode,
            PublishedAt = DateTime.UtcNow,
            StartAt = contest.StartAt,
            EndAt = contest.EndAt
        };
    }
}