using Application.UseCases.Score.Dtos;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed record ScoreIoiSubmissionQuery(Guid SubmissionId) : IRequest<IoiSubmissionScoreDto?>;
