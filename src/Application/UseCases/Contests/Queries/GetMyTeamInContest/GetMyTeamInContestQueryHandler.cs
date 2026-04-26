using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

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
        // VALIDATE CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        // =========================
        // CALL REPOSITORY
        // =========================
        var result = await _contestRepository.GetMyTeamInContestAsync(
            request.ContestId,
            userId
        );

        if (result == null)
        {
            Console.WriteLine("❌ No team found for this user in contest");
            return null;
        }

        Console.WriteLine($"✅ Team found: {result.TeamName} | Members: {result.TeamSize}");

        return result;
    }
}