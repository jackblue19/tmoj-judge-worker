using Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Queries.GetContestRanking
{
    public class GetContestRankingHandler
    : IRequestHandler<GetContestRankingQuery, List<(Guid UserId, int Rank)>>
    {
        private readonly IContestRepository _contestRepository;

        public GetContestRankingHandler(IContestRepository contestRepository)
        {
            _contestRepository = contestRepository;
        }

        public async Task<List<(Guid UserId, int Rank)>> Handle(
            GetContestRankingQuery request,
            CancellationToken ct)
        {
            var scoreboard = await _contestRepository.GetScoreboardAsync(request.ContestId);

            return scoreboard
                .Select(x => (x.TeamId, x.Rank))
                .ToList();
        }
    }
}
