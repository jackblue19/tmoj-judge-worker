using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemBySlugSpec : Specification<Problem>
{
    public ProblemBySlugSpec(string slug , Guid? excludingProblemId = null)
    {
        var normalizedSlug = slug.Trim().ToLower();

        Query.Where(x => x.Slug != null && x.Slug.ToLower() == normalizedSlug);

        if ( excludingProblemId.HasValue )
        {
            Query.Where(x => x.Id != excludingProblemId.Value);
        }
    }
}
