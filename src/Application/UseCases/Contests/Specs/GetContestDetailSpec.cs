using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class GetContestDetailSpec : Specification<Contest>
{
    public GetContestDetailSpec(Guid contestId)
    {
        Query
            .Where(x => x.Id == contestId)
            .Include(x => x.ContestProblems!)
                .ThenInclude(cp => cp.Problem);
    }
}