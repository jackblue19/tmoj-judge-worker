using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Favorite.Queries.GetUserPublicCollections;

public class GetUserPublicCollectionsHandler
    : IRequestHandler<GetUserPublicCollectionsQuery, PublicCollectionsResult>
{
    private readonly IFavoriteRepository _repo;

    public GetUserPublicCollectionsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<PublicCollectionsResult> Handle(
        GetUserPublicCollectionsQuery request,
        CancellationToken ct)
    {
        var baseQuery = _repo.QueryPublicCollections()
            .Where(x => x.UserId == request.UserId);

        var submissions = _repo.QuerySubmissions();

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PublicCollectionDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                IsVisibility = x.IsVisibility,
                CreatedAt = x.CreatedAt,

                OwnerId = x.UserId,
                OwnerName = x.User.Username,

                TotalItems = x.CollectionItems.Count(),

                ProblemCount = x.CollectionItems
                    .Where(ci => ci.ProblemId != null)
                    .Select(ci => ci.ProblemId)
                    .Distinct()
                    .Count(),

                ContestCount = x.CollectionItems
                    .Count(ci => ci.ContestId != null),

                SolvedCount = (
                    from ci in x.CollectionItems
                    where ci.ProblemId != null
                    join s in submissions on ci.ProblemId equals s.ProblemId
                    where s.UserId == request.UserId
                          && !s.IsDeleted
                          && s.StatusCode == "accepted"
                    select ci.ProblemId
                )
                .Distinct()
                .Count(),

                PreviewItems = x.CollectionItems
                    .OrderBy(ci => ci.OrderIndex)
                    .Take(3)
                    .Select(ci => new PreviewItemDto
                    {
                        ItemId = ci.Id,
                        ProblemId = ci.ProblemId,
                        ProblemTitle = ci.Problem != null ? ci.Problem.Title : null,
                        ContestId = ci.ContestId,
                        ContestTitle = ci.Contest != null ? ci.Contest.Title : null
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.SolvedPercent = item.ProblemCount == 0
                ? 0
                : Math.Round((double)item.SolvedCount / item.ProblemCount * 100, 2);
        }

        return new PublicCollectionsResult
        {
            Items = items,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize),
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}