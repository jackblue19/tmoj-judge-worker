using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemInPlanDetailSpec : Specification<Problem , ProblemDetailDto>
{
    public ProblemInPlanDetailSpec(Guid problemId)
    {
        Query
            .Where(x =>
                x.Id == problemId &&
                x.IsActive &&
                x.StatusCode == ProblemStatus.Published &&
                x.VisibilityCode == "in-plan") // ✅ Chỉ lấy bài in-plan

            .Where(x => x.Testsets.Any(t =>
                t.Type == TestsetType.Primary && t.IsActive));

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

            ProblemMode = x.ProblemMode ,
            ProblemSource = x.ProblemSource ,
            UsedCount = x.UsedCount ,
            OriginId = x.OriginId ,

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
                .Where(t => t.Type == TestsetType.Primary && t.IsActive)
                .OrderBy(t => t.CreatedAt)
                .Select(t => (Guid?) t.Id)
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
                    Icon = t.Icon
                })
                .ToList()
        })
        .AsNoTracking();
    }
}
