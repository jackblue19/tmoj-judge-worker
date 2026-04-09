using Application.UseCases.Contests.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Contests.Queries
{
    public class GetContestProblemsQuery : IRequest<List<ContestProblemDto>>
    {
        public Guid ContestId { get; set; }

        public GetContestProblemsQuery(Guid contestId)
        {
            ContestId = contestId;
        }
    }
}
