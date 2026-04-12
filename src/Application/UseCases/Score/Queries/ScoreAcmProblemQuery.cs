using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreAcmProblemQuery(Guid ContestProblemId, Guid TeamId)
    : IRequest<AcmProblemScoreDto?>;
