using Ardalis.Specification;
using Application.UseCases.Problems.Dtos;
using Domain.Entities;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class PublicProblemsPagedSpec : Specification<Problem , PublicProblemListItemDto>
{
    public PublicProblemsPagedSpec(int page , int pageSize , string? search , string? difficulty)
    {
        Query.Where(x =>
            x.IsActive &&
            x.StatusCode == "published" &&
            x.VisibilityCode == "public");

        if ( !string.IsNullOrWhiteSpace(search) )
        {
            var keyword = search.Trim();
            Query.Where(x =>
                x.Title.Contains(keyword) ||
                (x.Slug != null && x.Slug.Contains(keyword)));
        }

        if ( !string.IsNullOrWhiteSpace(difficulty) )
        {
            var normalizedDifficulty = difficulty.Trim();
            Query.Where(x => x.Difficulty == normalizedDifficulty);
        }

        Query.OrderBy(x => x.DisplayIndex ?? int.MaxValue)
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
            AcceptancePercent = x.AcceptancePercent ,
            TimeLimitMs = x.TimeLimitMs ,
            MemoryLimitKb = x.MemoryLimitKb ,
            DisplayIndex = x.DisplayIndex ,
            PublishedAt = x.PublishedAt ,
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