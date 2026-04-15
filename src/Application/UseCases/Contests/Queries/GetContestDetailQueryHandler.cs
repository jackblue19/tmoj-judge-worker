using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Policies;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestDetailQueryHandler
    : IRequestHandler<GetContestDetailQuery, ContestDetailDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IContestStatusService _statusService;

    public GetContestDetailQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IContestStatusService statusService)
    {
        _contestRepo = contestRepo;
        _statusService = statusService;
    }

    public async Task<ContestDetailDto> Handle(
        GetContestDetailQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.FirstOrDefaultAsync(
            new GetContestDetailSpec(request.ContestId),
            ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var isFrozen = FreezeContestPatch.IsFrozen(contest);

        var status = _statusService.GetStatus(contest.StartAt, contest.EndAt);
        var phase = _statusService.GetPhase(contest.StartAt, contest.EndAt);
        var canJoin = _statusService.CanJoin(contest.StartAt, contest.EndAt);

        var problems = (contest.ContestProblems ?? new List<ContestProblem>())
            .Where(cp => cp.IsActive)
            .OrderBy(cp => cp.DisplayIndex ?? cp.Ordinal ?? 999)
            .ThenBy(cp => cp.Alias)
            .ToList();

        // 🔥 FREEZE ENFORCEMENT (IMPORTANT)
        FreezeContestPatch.EnsureViewAllowed(contest);

        return new ContestDetailDto
        {
            Id = contest.Id,
            Title = contest.Title ?? "",
            Description = contest.DescriptionMd ?? "",
            Slug = !string.IsNullOrWhiteSpace(contest.Slug)
                ? contest.Slug
                : $"{SlugHelper.Generate(contest.Title ?? "contest")}-{contest.Id.ToString()[..6]}",

            Visibility = contest.VisibilityCode ?? "private",
            ContestType = contest.ContestType ?? "icpc",
            AllowTeams = contest.AllowTeams,

            Status = status,
            Phase = phase,

            IsPublished = contest.VisibilityCode == "public",

            IsFrozen = isFrozen,

            CanJoin = canJoin && !isFrozen,
            IsRegistered = false,
            HasLeaderboard = true,

            StartAt = contest.StartAt,
            EndAt = contest.EndAt,
            DurationMinutes = (int)(contest.EndAt - contest.StartAt).TotalMinutes,

            ProblemCount = problems.Count,
            TotalPoints = problems.Sum(p => p.Points ?? 0),

            Problems = isFrozen
                ? new List<ContestProblemDto>()   // 🔥 NO DATA LEAK
                : problems.Select(cp => new ContestProblemDto
                {
                    Id = cp.Id,
                    ProblemId = cp.ProblemId,
                    Title = cp.Problem != null ? cp.Problem.Title : "Unknown",
                    Alias = cp.Alias ?? "",
                    Ordinal = cp.Ordinal,
                    DisplayIndex = cp.DisplayIndex,
                    Points = cp.Points ?? 0,
                    TimeLimitMs = cp.TimeLimitMs,
                    MemoryLimitKb = cp.MemoryLimitKb,
                    Status = "not_started"
                }).ToList()
        };
    }
}