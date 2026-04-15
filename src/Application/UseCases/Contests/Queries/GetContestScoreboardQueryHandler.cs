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

        // =========================
        // VALIDATE CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        // =========================
        // GET SCOREBOARD (REPO HANDLE FREEZE)
        // =========================
        var result = await _contestRepository.GetScoreboardAsync(request.ContestId);

        Console.WriteLine($"Returned rows: {result.Count}");

        return result;
    }
}