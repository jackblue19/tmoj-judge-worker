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
        // =========================
        // CHECK CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        // =========================
        // GET PROBLEMS
        // =========================
        var data = await _repo.ListAsync(
            new ContestProblemByContestSpec(request.ContestId),
            ct);

        var problems = data
            .Where(x => x.IsActive)
            .Where(x => x.AccessMode != "hidden")
            .OrderBy(x => x.DisplayIndex ?? x.Ordinal ?? 999)
            .ThenBy(x => x.Alias)
            .ToList();

        return problems.Select(x => new ContestProblemDto
        {
            Id = x.Id,
            ProblemId = x.ProblemId,

            Title = x.Problem != null
                ? x.Problem.Title
                : "Unknown",

            Alias = x.Alias ?? "",
            Ordinal = x.Ordinal,
            DisplayIndex = x.DisplayIndex,

            Points = x.Points ?? 0,
            TimeLimitMs = x.TimeLimitMs,
            MemoryLimitKb = x.MemoryLimitKb,

            AccessMode = x.AccessMode,

            Status = "not_started"
        }).ToList();
    }
}