using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestScoreboardQueryHandler
    : IRequestHandler<GetContestScoreboardQuery, List<ContestScoreboardDto>>
{
    private readonly IContestRepository _contestRepository;
    private readonly IReadRepository<Contest, Guid> _contestRepo;

    public GetContestScoreboardQueryHandler(
        IContestRepository contestRepository,
        IReadRepository<Contest, Guid> contestRepo)
    {
        _contestRepository = contestRepository;
        _contestRepo = contestRepo;
    }

    public async Task<List<ContestScoreboardDto>> Handle(
        GetContestScoreboardQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("=== GET SCOREBOARD ===");
        Console.WriteLine($"ContestId: {request.ContestId}");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        var isFrozen =
            contest.FreezeAt.HasValue &&
            now >= contest.FreezeAt.Value;

        Console.WriteLine($"IsFrozen: {isFrozen}");

        var result = await _contestRepository.GetScoreboardAsync(request.ContestId);

        // =========================
        // FREEZE MODE NOTE
        // =========================
        if (isFrozen)
        {
            // Không mutate result ở đây
            // Repo should already handle filtering submissions if needed
            Console.WriteLine("⚠ Scoreboard is FROZEN (read-only view)");
        }

        Console.WriteLine($"Returned rows: {result.Count}");

        return result;
    }
}