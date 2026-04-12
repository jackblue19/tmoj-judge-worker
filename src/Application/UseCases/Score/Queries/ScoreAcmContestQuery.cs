using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreAcmContestQuery(Guid ContestId, Guid TeamId)
    : IRequest<AcmContestScoreDto?>;
