using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetPublicContestScoreboard;

public record GetPublicContestScoreboardQuery(Guid ContestId)
    : IRequest<GetContestLeaderboardResponse>;
