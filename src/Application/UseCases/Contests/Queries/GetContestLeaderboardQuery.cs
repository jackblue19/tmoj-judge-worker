using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Contests.Queries
{
    public class GetContestLeaderboardQuery : IRequest<GetContestLeaderboardResponse>
    {
        public Guid ContestId { get; set; }
    }
}