using Application.Common.Interfaces;
using Application.UseCases.Ranking.Dtos;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetGlobalLeaderboard;

public class GetGlobalLeaderboardQueryHandler
    : IRequestHandler<GetGlobalLeaderboardQuery, GlobalLeaderboardDto>
{
    private readonly IRankingRepository _repo;

    public GetGlobalLeaderboardQueryHandler(IRankingRepository repo) => _repo = repo;

    public Task<GlobalLeaderboardDto> Handle(GetGlobalLeaderboardQuery request, CancellationToken ct) =>
        _repo.GetGlobalLeaderboardAsync(
            request.Page, request.PageSize, request.Search,
            request.SubjectId, request.SemesterId, ct);
}
