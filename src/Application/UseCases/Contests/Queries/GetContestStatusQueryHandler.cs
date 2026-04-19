using Application.Common.Policies;
using Application.UseCases.Contests.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestStatusQueryHandler
    : IRequestHandler<GetContestStatusQuery, ContestStatusDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;

    public GetContestStatusQueryHandler(IReadRepository<Contest, Guid> contestRepo)
    {
        _contestRepo = contestRepo;
    }

    public async Task<ContestStatusDto> Handle(
        GetContestStatusQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        // Ưu tiên status DB. Nếu chưa migrate (rỗng) thì suy ra từ time.
        var status = !string.IsNullOrWhiteSpace(contest.Status)
            ? contest.Status
            : InferStatus(contest, now);

        var scoreboardMode = !string.IsNullOrWhiteSpace(contest.ScoreboardMode)
            ? contest.ScoreboardMode
            : InferScoreboardMode(contest, now);

        var isFrozen = FreezeContestPatch.IsFrozen(contest);

        var canSubmit =
            status == "running"
            && now <= contest.EndAt;

        var canViewProblems =
            status != "draft"
            && status != "cancelled";

        return new ContestStatusDto
        {
            ContestStatus   = status,
            ScoreboardMode  = scoreboardMode,
            ServerTime      = now,
            StartAt         = contest.StartAt,
            EndAt           = contest.EndAt,
            FreezeAt        = contest.FreezeAt,
            UnfreezeAt      = contest.UnfreezeAt,
            FinalizedAt     = contest.FinalizedAt,
            CanSubmit       = canSubmit,
            CanViewProblems = canViewProblems,
            IsFrozen        = isFrozen,
            IsFinalized     = status == "finalized"
        };
    }

    private static string InferStatus(Contest c, DateTime now)
    {
        if (!c.IsActive)              return "cancelled";
        if (now < c.StartAt)          return "scheduled";
        if (now <= c.EndAt)           return "running";
        return "closed";
    }

    private static string InferScoreboardMode(Contest c, DateTime now)
    {
        if (FreezeContestPatch.IsFrozen(c)) return "frozen";
        if (c.FinalizedAt.HasValue)         return "final";
        return "live";
    }
}
