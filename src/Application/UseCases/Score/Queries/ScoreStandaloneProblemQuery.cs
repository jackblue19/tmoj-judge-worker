using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreStandaloneProblemQueryResult(
    bool NotFound,
    bool BelongsToContest,
    IoiStandaloneProblemScoreDto? Data);

public sealed record ScoreStandaloneProblemQuery(Guid SubmissionId)
    : IRequest<ScoreStandaloneProblemQueryResult>;
