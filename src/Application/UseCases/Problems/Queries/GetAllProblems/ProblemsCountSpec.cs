using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Queries.GetAllProblems;

public sealed class ProblemsCountSpec : Specification<Problem>
{
    public ProblemsCountSpec(string? difficulty , string? status)
    {
        Query.Where(x => x.IsActive);

        if ( !string.IsNullOrWhiteSpace(difficulty) )
            Query.Where(x => x.Difficulty == difficulty);

        if ( !string.IsNullOrWhiteSpace(status) )
            Query.Where(x => x.StatusCode == status);
    }
}
