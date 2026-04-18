using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Queries.GetContestRanking
{
    public record GetContestRankingQuery(Guid ContestId)
    : IRequest<List<(Guid UserId, int Rank)>>;
}
