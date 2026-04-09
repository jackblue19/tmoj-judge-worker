using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;
namespace Application.UseCases.Contests.Specs
{


    public class TestsetByProblemSpec : Specification<Testset>
    {
        public TestsetByProblemSpec(Guid problemId)
        {
            Query
                .Where(x => x.ProblemId == problemId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt);
        }
    }
}
