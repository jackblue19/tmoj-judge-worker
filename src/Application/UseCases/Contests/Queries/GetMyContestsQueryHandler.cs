using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetMyContestsQueryHandler
    : IRequestHandler<GetMyContestsQuery, List<MyContestDto>>
{
    private readonly IContestRepository _contestRepo;
    private readonly IContestStatusService _statusService;
    private readonly ICurrentUserService _currentUser;

    public GetMyContestsQueryHandler(
        IContestRepository contestRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _statusService = statusService;
        _currentUser = currentUser;
    }

    public async Task<List<MyContestDto>> Handle(
        GetMyContestsQuery request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        Console.WriteLine("=== GET MY CONTESTS (DETAILED) ===");
        Console.WriteLine($"UserId: {userId}");

        var contests = await _contestRepo.GetMyContestsDetailedAsync(userId);

        // 🔥 filter status nếu có
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            contests = contests
                .Where(x => x.Status.Equals(
                    request.Status,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return contests;
    }
}