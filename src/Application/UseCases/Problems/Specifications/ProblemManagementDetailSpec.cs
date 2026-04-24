using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Specifications;

public sealed class ProblemManagementDetailSpec : Specification<Problem , ProblemDetailDto>
{
    public ProblemManagementDetailSpec(Guid problemId , Guid currentUserId , bool isAdmin)
    {
        Query
            .Where(x =>
                x.Id == problemId &&
                x.IsActive &&
                (isAdmin || x.CreatedBy == currentUserId));

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
                .Where(t => t.IsActive)
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