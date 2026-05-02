using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Queries.GetProblemBanks;
using Ardalis.Specification;
using Domain.Constants;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetInPlanProblems;

public sealed class InPlanProblemsPagedSpec : Specification<Problem , ProblemBankListItemDto>
{
    public InPlanProblemsPagedSpec(
        int page ,
        int pageSize ,
        string? search ,
        string? difficulty)
    {
        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == ProblemStatusCodes.Published &&
            x.VisibilityCode == ProblemVisibilityCodes.InPlan);

        if ( !string.IsNullOrWhiteSpace(search) )
        {
            var keyword = search.Trim().ToLower();

            Query.Where(x =>
                (x.Title != null && x.Title.ToLower().Contains(keyword)) ||
                (x.Slug != null && x.Slug.ToLower().Contains(keyword)));
        }

        if ( !string.IsNullOrWhiteSpace(difficulty) )
        {
            Query.Where(x => x.Difficulty == difficulty);
        }

        Query
            .OrderBy(x => x.DisplayIndex ?? int.MaxValue)
            .ThenBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        Query.Select(x => new ProblemBankListItemDto
        {
            Id = x.Id ,
            Slug = x.Slug ?? string.Empty ,
            Title = x.Title ,

            Difficulty = x.Difficulty ,
            TypeCode = x.TypeCode ,
            VisibilityCode = x.VisibilityCode ,
            ScoringCode = x.ScoringCode ,

            ProblemMode = x.ProblemMode ,
            ProblemSource = x.ProblemSource ,

            AcceptancePercent = x.AcceptancePercent ,
            TimeLimitMs = x.TimeLimitMs ,
            MemoryLimitKb = x.MemoryLimitKb ,
            DisplayIndex = x.DisplayIndex ,
            PublishedAt = x.PublishedAt ,

            Tags = x.Tags
                    .Where(t => t.IsActive)
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
        })
            .AsNoTracking();
    }
}
