using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestProblemsQueryHandler
    : IRequestHandler<GetContestProblemsQuery, List<ContestProblemDto>>
{
    private readonly IReadRepository<ContestProblem, Guid> _repo;
    private readonly IReadRepository<Contest, Guid> _contestRepo;

    public GetContestProblemsQueryHandler(
        IReadRepository<ContestProblem, Guid> repo,
        IReadRepository<Contest, Guid> contestRepo)
    {
        _repo = repo;
        _contestRepo = contestRepo;
    }

    public async Task<List<ContestProblemDto>> Handle(
        GetContestProblemsQuery request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // =========================
        // GET CONTEST (FREEZE CHECK)
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var isFrozen =
            contest.FreezeAt.HasValue &&
            now >= contest.FreezeAt.Value;

        var data = await _repo.ListAsync(
            new ContestProblemByContestSpec(request.ContestId),
            ct);

        var problems = data
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayIndex ?? x.Ordinal ?? 999)
            .ThenBy(x => x.Alias)
            .ToList();

        return problems.Select(x => new ContestProblemDto
        {
            Id = x.Id,
            ProblemId = x.ProblemId,

            // =========================
            // FREEZE MASK
            // =========================
            Title = isFrozen
                ? "Frozen"
                : (x.Problem != null ? x.Problem.Title : "Unknown"),

            Alias = x.Alias ?? "",
            Ordinal = x.Ordinal,
            DisplayIndex = x.DisplayIndex,

            Points = x.Points ?? 0,
            TimeLimitMs = x.TimeLimitMs,
            MemoryLimitKb = x.MemoryLimitKb,

            // =========================
            // STATUS MASK
            // =========================
            Status = isFrozen ? "frozen" : "not_started"
        }).ToList();
    }
}