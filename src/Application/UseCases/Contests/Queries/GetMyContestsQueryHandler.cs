using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetMyContestsQueryHandler
    : IRequestHandler<GetMyContestsQuery, List<MyContestDto>>
{
    private readonly IContestRepository _contestRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyContestsQueryHandler(
        IContestRepository contestRepository,
        ICurrentUserService currentUser)
    {
        _contestRepository = contestRepository;
        _currentUser = currentUser;
    }

    public async Task<List<MyContestDto>> Handle(
        GetMyContestsQuery request,
        CancellationToken ct)
    {
        // =========================
        // AUTH CHECK
        // =========================
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        Console.WriteLine("=== GET MY CONTESTS ===");
        Console.WriteLine($"UserId: {userId}");

        // =========================
        // ✅ USE CORRECT METHOD
        // =========================
        var contests = await _contestRepository
            .GetMyContestsDetailedAsync(userId);

        var now = DateTime.UtcNow;

        // =========================
        // 🔥 ENRICH BUSINESS RULE
        // =========================
        foreach (var c in contests)
        {
            var isEnded = now > c.EndAt;

            // status
            c.Status =
                now < c.StartAt ? "upcoming" :
                now <= c.EndAt ? "running" :
                "ended";

            // phase
            c.Phase =
                now < c.StartAt ? "BEFORE" :
                now <= c.EndAt ? "CODING" :
                "FINISHED";

            // optional flags
            c.CanJoin = now < c.StartAt;
            c.CanRegister = now < c.StartAt;
            c.CanUnregister = now < c.StartAt;

            // 🔥 RULE END CONTEST
            if (isEnded)
            {
                // =========================
                // STATUS OVERRIDE
                // =========================
                c.Status = "ended";
                c.Phase = "FINISHED";

                // =========================
                // LOCK ACTIONS
                // =========================
                c.CanJoin = false;
                c.CanRegister = false;
                c.CanUnregister = false;

                // =========================
                // BUSINESS RULE
                // =========================
                // ✅ ĐƯỢC PHÉP:
                // - xem verdict
                // - xem rank
                // - xem score
                // - xem submission list (metadata)

                // ❌ KHÔNG ĐƯỢC:
                // - xem source code
                // - submit thêm

                // 👉 backend KHÔNG cần sửa DTO
                // 👉 FE chỉ cần check:
                // if (contest.status === "ended") => hide source code
            }
        }

        Console.WriteLine($"✅ Total contests: {contests.Count}");

        return contests;
    }
}