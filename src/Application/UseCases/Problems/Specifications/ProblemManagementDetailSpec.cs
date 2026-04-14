using Application.UseCases.Problems.Dtos;
using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemManagementDetailSpec
    : Specification<Problem , ProblemDetailDto>
{
    public ProblemManagementDetailSpec(
        Guid problemId ,
        Guid currentUserId ,
        bool isAdmin)
    {
        Query
            .Where(x => x.Id == problemId)

            .Where(x =>
                isAdmin ||
                x.CreatedBy == currentUserId)

            .Include(x => x.Testsets.Where(t => t.IsActive))
            .Include(x => x.Tags)
            .Include(x => x.ProblemStat)

            .AsNoTracking();
    }
}