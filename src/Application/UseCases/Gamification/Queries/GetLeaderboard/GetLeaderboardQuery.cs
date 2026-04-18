using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetLeaderboard;

public class GetLeaderboardQuery : IRequest<LeaderboardResponseDto>
{
    public string Type { get; set; } = "exp";
}