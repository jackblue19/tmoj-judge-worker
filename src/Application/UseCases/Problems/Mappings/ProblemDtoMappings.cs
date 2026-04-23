using Application.UseCases.Problems.Dtos;
using Domain.Entities;

namespace Application.UseCases.Problems.Mappings;

public static class ProblemDtoMappings
{
    public static ProblemTagDto ToDto(this Tag tag)
    {
        return new ProblemTagDto
        {
            Id = tag.Id ,
            Name = tag.Name ,
            Slug = tag.Slug ,
            Description = tag.Description ,
            Color = tag.Color ,
            Icon = tag.Icon ,
            IsActive = tag.IsActive
        };
    }

    public static ProblemSummaryDto ToSummaryDto(this Problem problem)
    {
        return new ProblemSummaryDto
        {
            Id = problem.Id ,
            Title = problem.Title ,
            Slug = problem.Slug ,
            StatusCode = problem.StatusCode ,
            Difficulty = problem.Difficulty ,
            TypeCode = problem.TypeCode ,
            VisibilityCode = problem.VisibilityCode ,
            ScoringCode = problem.ScoringCode ,
            TimeLimitMs = problem.TimeLimitMs ,
            MemoryLimitKb = problem.MemoryLimitKb ,
            IsActive = problem.IsActive ,
            CreatedAt = problem.CreatedAt ,
            UpdatedAt = problem.UpdatedAt ,
            PublishedAt = problem.PublishedAt ,
            Tags = problem.Tags
                .OrderBy(x => x.Name)
                .Select(x => x.ToDto())
                .ToList()
        };
    }

    public static ProblemDetailDto ToDetailDto(this Problem problem , string? statementAccessUrl = null)
    {
        return new ProblemDetailDto
        {
            Id = problem.Id ,
            Title = problem.Title ,
            Slug = problem.Slug ,

            StatusCode = problem.StatusCode ,
            Difficulty = problem.Difficulty ,
            TypeCode = problem.TypeCode ,
            VisibilityCode = problem.VisibilityCode ,
            ScoringCode = problem.ScoringCode ,

            ProblemMode = problem.ProblemMode ,
            ProblemSource = problem.ProblemSource ,
            UsedCount = problem.UsedCount ,
            OriginId = problem.OriginId ,

            DescriptionMd = problem.DescriptionMd ,
            AcceptancePercent = problem.AcceptancePercent ,
            DisplayIndex = problem.DisplayIndex ,
            TimeLimitMs = problem.TimeLimitMs ,
            MemoryLimitKb = problem.MemoryLimitKb ,

            IsActive = problem.IsActive ,

            CreatedAt = problem.CreatedAt ,
            CreatedBy = problem.CreatedBy ,
            UpdatedAt = problem.UpdatedAt ,
            UpdatedBy = problem.UpdatedBy ,

            ApprovedByUserId = problem.ApprovedByUserId ,
            ApprovedAt = problem.ApprovedAt ,
            PublishedAt = problem.PublishedAt ,

            StatementSourceCode = problem.StatementSourceCode ,
            StatementContentType = problem.StatementContentType ,
            StatementFileName = problem.StatementFileName ,
            StatementAccessUrl = statementAccessUrl ,

            PrimaryTestsetId = problem.Testsets
                .Where(x => x.IsActive)
                .OrderBy(x => x.CreatedAt)
                .Select(x => (Guid?) x.Id)
                .FirstOrDefault() ,

            Tags = problem.Tags
                .OrderBy(x => x.Name)
                .Select(x => x.ToDto())
                .ToList()
        };
    }
}