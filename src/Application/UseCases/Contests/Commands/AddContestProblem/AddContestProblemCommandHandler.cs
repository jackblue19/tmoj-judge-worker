using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Commands;

public class AddContestProblemCommandHandler
    : IRequestHandler<AddContestProblemCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<Problem, Guid> _problemRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpReadRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _cpWriteRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AddContestProblemCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<Problem, Guid> problemRepo,
        IReadRepository<ContestProblem, Guid> cpReadRepo,
        IWriteRepository<ContestProblem, Guid> cpWriteRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _problemRepo = problemRepo;
        _cpReadRepo = cpReadRepo;
        _cpWriteRepo = cpWriteRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddContestProblemCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // ======================
        // 1. CHECK CONTEST
        // ======================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);
        if (contest == null)
            throw new Exception("Contest not found");

        if (contest.StartAt <= DateTime.UtcNow)
            throw new Exception("Cannot modify contest after start");

        // ======================
        // 2. CHECK PROBLEM
        // ======================
        var problem = await _problemRepo.GetByIdAsync(request.ProblemId, ct);
        if (problem == null)
            throw new Exception("Problem not found");

        // ======================
        // 3. GET EXISTING CONTEST PROBLEMS (FIXED SPEC USAGE)
        // ======================
        var existing = await _cpReadRepo.ListAsync(
            new ContestProblemByContestSpec(request.ContestId),
            ct);

        if (existing.Any(x => x.ProblemId == request.ProblemId))
            throw new Exception("Problem already exists in contest");

        // ======================
        // 4. AUTO ALIAS A, B, C...
        // ======================
        var alias = request.Alias
            ?? ((char)('A' + existing.Count)).ToString();

        // ======================
        // 5. CREATE ENTITY
        // ======================
        var entity = new ContestProblem
        {
            Id = Guid.NewGuid(),

            ContestId = request.ContestId,
            ProblemId = request.ProblemId,

            Alias = alias,
            Ordinal = request.Ordinal,
            DisplayIndex = request.DisplayIndex,

            Points = request.Points ?? 100,
            MaxScore = request.MaxScore ?? 100,

            TimeLimitMs = request.TimeLimitMs ?? problem.TimeLimitMs,
            MemoryLimitKb = request.MemoryLimitKb ?? problem.MemoryLimitKb,

            // FIX: Problem KHÔNG có OutputLimitKb
            OutputLimitKb = request.OutputLimitKb,

            PenaltyPerWrong = request.PenaltyPerWrong ?? 20,
            ScoringCode = request.ScoringCode ?? contest.ContestType,

            OverrideTestsetId = request.OverrideTestsetId,

            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _cpWriteRepo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return entity.Id;
    }
}