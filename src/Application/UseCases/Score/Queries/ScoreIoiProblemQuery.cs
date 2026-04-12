using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreIoiProblemQuery(Guid ContestProblemId, Guid TeamId)
    : IRequest<IoiProblemScoreDto?>;
