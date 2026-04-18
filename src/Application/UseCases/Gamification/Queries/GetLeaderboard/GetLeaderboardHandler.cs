using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Gamification.Queries.GetLeaderboard;

public class GetLeaderboardHandler
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardItemDto>>
{
    private readonly IGamificationRepository _repo;
    private readonly IUserRepository _userRepo;

    public GetLeaderboardHandler(
        IGamificationRepository repo,
        IUserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    public async Task<List<LeaderboardItemDto>> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var data = await _repo.GetLeaderboardAsync(request.Type);

        var userIds = data.Select(x => x.UserId).ToList();

        var users = await _userRepo.GetUsersByIdsAsync(userIds);

        var result = data
            .Select((x, index) =>
            {
                var user = users.FirstOrDefault(u => u.UserId == x.UserId);

                return new LeaderboardItemDto
                {
                    UserId = x.UserId,
                    DisplayName = user?.DisplayName ?? "Unknown",
                    AvatarUrl = user?.AvatarUrl,
                    Value = x.Value,
                    Rank = index + 1
                };
            })
            .ToList();

        return result;
    }
}