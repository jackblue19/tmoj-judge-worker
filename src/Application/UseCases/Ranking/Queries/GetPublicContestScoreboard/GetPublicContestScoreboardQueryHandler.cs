using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using MediatR;

namespace Application.UseCases.Ranking.Queries.GetPublicContestScoreboard;

public class GetPublicContestScoreboardQueryHandler
    : IRequestHandler<GetPublicContestScoreboardQuery, GetContestLeaderboardResponse>
{
    private readonly IRankingRepository _rankingRepo;
    private readonly IMediator _mediator;

    public GetPublicContestScoreboardQueryHandler(IRankingRepository rankingRepo, IMediator mediator)
    {
        _rankingRepo = rankingRepo;
        _mediator = mediator;
    }

    public async Task<GetContestLeaderboardResponse> Handle(
        GetPublicContestScoreboardQuery request, CancellationToken ct)
    {
        var exists = await _rankingRepo.ContestExistsAsync(request.ContestId, ct);
        if (!exists)
            throw new KeyNotFoundException("Contest not found.");

        return await _mediator.Send(
            new GetContestLeaderboardQuery { ContestId = request.ContestId }, ct);
    }
}
