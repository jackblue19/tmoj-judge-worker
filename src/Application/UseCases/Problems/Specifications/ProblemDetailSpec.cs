using Ardalis.Specification;
using Application.UseCases.Problems.Dtos;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemDetailSpec : Specification<Problem , ProblemDetailDto>
{
    public ProblemDetailSpec(Guid problemId)
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
            AcceptancePercent = x.AcceptancePercent ,
            DisplayIndex = x.DisplayIndex ,
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
            StatementAccessUrl = null ,
            PrimaryTestsetId = x.Testsets
                .Where(ts => ts.IsActive)
                .OrderBy(ts => ts.CreatedAt)
                .Select(ts => (Guid?) ts.Id)
                .FirstOrDefault() ,
            Tags = x.Tags
                .OrderBy(t => t.Name)
                .Select(t => new ProblemTagDto
                {
                    Id = t.Id ,
                    Name = t.Name ,
                    Slug = t.Slug ,
                    Description = t.Description ,
                    Color = t.Color ,
                    Icon = t.Icon ,
                    IsActive = t.IsActive
                })
                .ToList()
        });
    }
}