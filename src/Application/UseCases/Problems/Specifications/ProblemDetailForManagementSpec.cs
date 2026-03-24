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

public sealed class ProblemDetailForManagementSpec : Specification<Problem , ProblemDetailDto>
{
    public ProblemDetailForManagementSpec(Guid problemId)
    {
        Query.Where(x => x.Id == problemId);

        Query.Select(x => new ProblemDetailDto
        {
            Id = x.Id ,
            Title = x.Title ,
            Slug = x.Slug ,
            StatusCode = x.StatusCode ,
            Difficulty = x.Difficulty ,
            TypeCode = x.TypeCode ,
            VisibilityCode = x.VisibilityCode ,
            ScoringCode = x.ScoringCode ,
            DescriptionMd = x.DescriptionMd ,
            TimeLimitMs = x.TimeLimitMs ,
            MemoryLimitKb = x.MemoryLimitKb ,
            IsActive = x.IsActive ,
            CreatedAt = x.CreatedAt ,
            CreatedBy = x.CreatedBy ,
            UpdatedAt = x.UpdatedAt ,
            UpdatedBy = x.UpdatedBy ,
            ApprovedByUserId = x.ApprovedByUserId ,
            ApprovedAt = x.ApprovedAt ,
            PublishedAt = x.PublishedAt ,
            Tags = x.Tags
                .OrderBy(t => t.Name)
                .Select(t => new ProblemTagDto
                {
                    Id = t.Id , 
                    Name = t.Name ,
                    Slug = t.Slug
                })
                .ToList()
        });
    }
}