using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetMyTeamInContestQueryHandler
    : IRequestHandler<GetMyTeamInContestQuery, MyTeamInContestDto?>
{
    private readonly IContestRepository _contestRepository;
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly ICurrentUserService _currentUser;

    public GetMyTeamInContestQueryHandler(
        IContestRepository contestRepository,
        IReadRepository<Contest, Guid> contestRepo,
        ICurrentUserService currentUser)
    {
        _contestRepository = contestRepository;
        _contestRepo = contestRepo;
        _currentUser = currentUser;
    }

    public async Task<MyTeamInContestDto?> Handle(
        GetMyTeamInContestQuery request,
        CancellationToken ct)
    {
        // =========================
        // AUTH CHECK
        // =========================
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        Console.WriteLine("=== GET MY TEAM IN CONTEST ===");
        Console.WriteLine($"UserId: {userId}");
        Console.WriteLine($"ContestId: {request.ContestId}");

        // =========================
        // GET CONTEST (FREEZE CHECK)
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        var isFrozen =
            contest.FreezeAt.HasValue &&
            now >= contest.FreezeAt.Value;

        Console.WriteLine($"IsFrozen: {isFrozen}");

        // =========================
        // CALL REPOSITORY
        // =========================
        var team = await _contestRepository.GetMyTeamInContestAsync(
            request.ContestId,
            userId
        );

        // =========================
        // RESULT LOGIC
        // =========================
        if (team == null)
        {
            Console.WriteLine("❌ No team found for this user in contest");
            return null;
        }

        // =========================
        // FREEZE ENRICHMENT (SAFE)
        // =========================
        if (isFrozen)
        {
            // optional: you can later mask rank/score here
            Console.WriteLine("⚠ Team view is in FROZEN mode");
        }

        Console.WriteLine($"✅ Team found: {team.TeamName} | Members: {team.TeamSize}");

        return team;
    }
}