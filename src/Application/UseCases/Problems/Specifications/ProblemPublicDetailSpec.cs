using Application.UseCases.Problems.Constants;
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

public sealed class ProblemPublicDetailSpec
    : Specification<Problem , ProblemDetailDto>
{
    public ProblemPublicDetailSpec(Guid problemId)
    {
        Query
            .Where(x =>
                x.Id == problemId &&
                x.IsActive &&
                x.StatusCode == ProblemStatus.Published &&
                x.VisibilityCode == ProblemVisibility.Public)

            .Where(x => x.Testsets.Any(t =>
                t.Type == TestsetType.Primary && t.IsActive))

            .Include(x => x.Tags)
            .Include(x => x.ProblemStat)

            .AsNoTracking();
    }
}