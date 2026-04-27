using Application.Common.Interfaces;
using Application.UseCases.Ranking.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetPublicContests;

public class GetPublicContestsQueryHandler
    : IRequestHandler<GetPublicContestsQuery, List<PublicContestSummaryDto>>
{
    private readonly IRankingRepository _repo;

    public GetPublicContestsQueryHandler(IRankingRepository repo) => _repo = repo;

    public Task<List<PublicContestSummaryDto>> Handle(GetPublicContestsQuery request, CancellationToken ct) =>
        _repo.GetPublicContestsAsync(ct);
}
