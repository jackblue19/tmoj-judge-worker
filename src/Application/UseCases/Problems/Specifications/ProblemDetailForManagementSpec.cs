using Application.UseCases.Problems.Dtos;
using Ardalis.Specification;
using Domain.Entities;

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

            StatementSourceCode = x.StatementSourceCode ,
            StatementContentType = x.StatementContentType ,
            StatementFileName = x.StatementFileName ,

            // backend endpoint để FE gọi download/view
            StatementAccessUrl = x.StatementFileId != null
                ? $"/api/v2/problems/{x.Id}/statement"
                : null ,

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