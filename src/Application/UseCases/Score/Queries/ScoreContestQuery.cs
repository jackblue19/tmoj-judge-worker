using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreContestQuery(Guid ContestId, Guid TeamId)
    : IRequest<ContestScoreDto?>;
