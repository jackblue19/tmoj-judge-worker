using Application.UseCases.Ranking.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetPublicContests;

public record GetPublicContestsQuery(
    Guid? SubjectId = null,
    Guid? SemesterId = null
) : IRequest<List<PublicContestSummaryDto>>;
