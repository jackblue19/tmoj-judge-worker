using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Favorite.Queries.GetFavoriteProblems;

public class GetFavoriteProblemsHandler
    : IRequestHandler<GetFavoriteProblemsQuery, PagedResult<FavoriteProblemDto>>
{
    private readonly IFavoriteRepository _repo;

    public GetFavoriteProblemsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<FavoriteProblemDto>> Handle(
        GetFavoriteProblemsQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 GetFavoriteProblems START");

        var userId = request.UserId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        // =========================
        // FIX PAGINATION
        // =========================
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        // =========================
        // GET COLLECTION
        // =========================
        var collection = await _repo.GetUserCollectionByTypeAsync(
            userId,
            "problem_favorite"
        );

        if (collection == null)
        {
            Console.WriteLine("👉 No favorite collection found");

            return new PagedResult<FavoriteProblemDto>
            {
                Items = new List<FavoriteProblemDto>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = 0,
                TotalPages = 0
            };
        }

        Console.WriteLine($"👉 CollectionId: {collection.Id}");

        // =========================
        // QUERY
        // =========================
        var query = _repo.QueryFavoriteProblems(collection.Id)
            .Where(p =>
                p.IsActive && // 🔥 tránh trả problem bị disable
                (p.VisibilityCode == "public" || p.CreatedBy == userId) // 🔥 tránh leak private
            );

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt) // ⚠️ nếu muốn chuẩn hơn thì cần order theo CollectionItem.CreatedAt
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new FavoriteProblemDto
            {
                ProblemId = p.Id,
                Title = p.Title,
                Difficulty = p.Difficulty,
                TypeCode = p.TypeCode,
                StatusCode = p.StatusCode,
                IsFavorited = true
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        Console.WriteLine($"👉 Returned {items.Count}/{totalItems}");

        Console.WriteLine("🔥 GetFavoriteProblems END");

        return new PagedResult<FavoriteProblemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }
}