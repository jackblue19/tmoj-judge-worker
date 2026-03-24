using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Specifications;

public sealed class OwnedProblemForEditSpec : Specification<Problem>
{
    public OwnedProblemForEditSpec(Guid problemId)
    {
        Query.Where(x => x.Id == problemId)
             .Include(x => x.Tags);
    }
}
