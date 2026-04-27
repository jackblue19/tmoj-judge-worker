using Application.UseCases.Ranking.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetPublicContests;

public record GetPublicContestsQuery : IRequest<List<PublicContestSummaryDto>>;
