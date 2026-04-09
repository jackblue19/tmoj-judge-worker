using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Contests.Specs
{
    public class ContestProblemSpec : Specification<ContestProblem>
    {
        public ContestProblemSpec(Guid contestId)
        {
            Query.Where(x => x.ContestId == contestId);
        }
    }
}
