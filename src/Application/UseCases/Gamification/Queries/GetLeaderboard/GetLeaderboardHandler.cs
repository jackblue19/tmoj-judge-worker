using Application.Common.Interfaces;
using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetLeaderboard;

public class GetLeaderboardHandler
    : IRequestHandler<GetLeaderboardQuery, LeaderboardResponseDto>
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

    public async Task<LeaderboardResponseDto> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var data = await _repo.GetLeaderboardAsync(request.Type);

        var userIds = data.Select(x => x.UserId).ToList();

        var users = await _userRepo.GetUsersByIdsAsync(userIds);

        var userDict = users.ToDictionary(x => x.UserId);

        var items = data
            .Select((x, index) =>
            {
                userDict.TryGetValue(x.UserId, out var user);

                return new LeaderboardItemDto
                {
                    UserId = x.UserId,
                    DisplayName = user?.DisplayName ?? "Unknown",
                    AvatarUrl = user?.AvatarUrl,

                    Value = x.Value,

                    SolvedCount = request.Type == "exp" || request.Type == "solved"
                        ? x.Value
                        : 0,

                    Rank = index + 1
                };
            })
            .ToList();

        return new LeaderboardResponseDto
        {
            Type = request.Type,
            Total = items.Count,
            Top = items.Count,
            Items = items,
            Me = null // sau này add JWT thì fill
        };
    }
}