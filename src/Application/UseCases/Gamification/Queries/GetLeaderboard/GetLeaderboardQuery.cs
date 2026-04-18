using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetLeaderboard;

public class GetLeaderboardQuery : IRequest<List<LeaderboardItemDto>>
{
    public string Type { get; set; } = "exp"; // exp | streak | badge
}