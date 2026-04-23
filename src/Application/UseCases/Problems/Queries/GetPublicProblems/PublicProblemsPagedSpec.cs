using Application.UseCases.Problems.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class PublicProblemsPagedSpec : Specification<Problem , PublicProblemListItemDto>
{
    public PublicProblemsPagedSpec(
        int page ,
        int pageSize ,
        string? search ,
        string? difficulty)
    {
        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == "published" &&
            x.VisibilityCode == "public");

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

        Query.Select(x => new PublicProblemListItemDto
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