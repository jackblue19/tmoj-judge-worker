using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Favorite.Queries.GetMyCollections;

public class GetMyCollectionsHandler
    : IRequestHandler<GetMyCollectionsQuery, List<CollectionDto>>
{
    private readonly IFavoriteRepository _repo;

    public GetMyCollectionsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CollectionDto>> Handle(
        GetMyCollectionsQuery request,
        CancellationToken ct)
    {
        var userId = request.UserId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        var submissions = _repo.QuerySubmissions();

        var items = await _repo.QueryPublicCollections() // ❌ sai trước đó
            .Where(x => x.UserId == userId)              // ✅ đúng logic
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CollectionDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                IsVisibility = x.IsVisibility,
                CreatedAt = x.CreatedAt,

                // =========================
                // COUNT
                // =========================
                TotalItems = x.CollectionItems.Count(),

                ProblemCount = x.CollectionItems
                    .Count(ci => ci.ProblemId != null),

                ContestCount = x.CollectionItems
                    .Count(ci => ci.ContestId != null),

                // =========================
                // SOLVED COUNT (NO N+1)
                // =========================
                SolvedCount = (
                    from ci in x.CollectionItems
                    where ci.ProblemId != null
                    join s in submissions on ci.ProblemId equals s.ProblemId
                    where s.UserId == userId
                          && !s.IsDeleted
                          && s.StatusCode == "accepted"
                    select ci.ProblemId
                )
                .Distinct()
                .Count()
            })
            .ToListAsync(ct);

        // =========================
        // SOLVED PERCENT
        // =========================
        foreach (var item in items)
        {
            item.SolvedPercent = item.ProblemCount == 0
                ? 0
                : Math.Round((double)item.SolvedCount / item.ProblemCount * 100, 2);
        }

        return items;
    }
}