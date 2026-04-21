using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Favorite.Queries.GetFavoriteContests;

public class GetFavoriteContestsHandler
    : IRequestHandler<GetFavoriteContestsQuery, PagedResult<FavoriteContestDto>>
{
    private readonly IFavoriteRepository _repo;

    public GetFavoriteContestsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<FavoriteContestDto>> Handle(
        GetFavoriteContestsQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 GetFavoriteContests START");

        var userId = request.UserId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        // =========================
        // FIX PAGE
        // =========================
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        // =========================
        // GET COLLECTION
        // =========================
        var collection = await _repo.GetUserCollectionByTypeAsync(
            userId,
            "contest_favorite"
        );

        if (collection == null)
        {
            Console.WriteLine("👉 No contest favorite collection");

            return new PagedResult<FavoriteContestDto>
            {
                Items = new List<FavoriteContestDto>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = 0,
                TotalPages = 0
            };
        }

        Console.WriteLine($"👉 CollectionId: {collection.Id}");

        // =========================
        // QUERY (IQueryable)
        // =========================
        var query = _repo.QueryFavoriteContests(collection.Id);

        // ⚠️ IMPORTANT: dùng async hết
        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FavoriteContestDto
            {
                ContestId = x.Id,
                Title = x.Title,
                Slug = x.Slug,
                Description = x.DescriptionMd,

                VisibilityCode = x.VisibilityCode,
                ContestType = x.ContestType,

                AllowTeams = x.AllowTeams,

                StartAt = x.StartAt,
                EndAt = x.EndAt,

                Status = x.Status,
                ScoreboardMode = x.ScoreboardMode,

                IsVirtual = x.IsVirtual,

                IsFavorited = true
            })
            .ToListAsync(ct);

        Console.WriteLine($"👉 Returned {items.Count}/{totalItems} contests");

        Console.WriteLine("🔥 GetFavoriteContests END");

        return new PagedResult<FavoriteContestDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
    }
}