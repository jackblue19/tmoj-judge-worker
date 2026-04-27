using Application.UseCases.Ranking.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetGlobalLeaderboard;

public record GetGlobalLeaderboardQuery(
    int Page,
    int PageSize,
    string? Search
) : IRequest<GlobalLeaderboardDto>;
