using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestTeamByContestAndTeamSpec : Specification<ContestTeam>
{
    public ContestTeamByContestAndTeamSpec(Guid contestId, Guid teamId)
    {
        Query.Where(x => x.ContestId == contestId && x.TeamId == teamId);
    }
}