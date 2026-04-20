using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Application.UseCases.Favorite.Queries.GetCollectionDetail;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetCollectionDetail;

public class GetCollectionDetailHandler
    : IRequestHandler<GetCollectionDetailQuery, CollectionDetailDto>
{
    private readonly IFavoriteRepository _repo;

    public GetCollectionDetailHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<CollectionDetailDto> Handle(
        GetCollectionDetailQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 GetCollectionDetail START");

        var userId = request.UserId;
        var collectionId = request.CollectionId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        if (collectionId == Guid.Empty)
            throw new Exception("CollectionId invalid");

        // =========================
        // GET COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(collectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        // =========================
        // PERMISSION
        // =========================
        if (!collection.IsVisibility && collection.UserId != userId)
            throw new Exception("You do not have permission to view this collection");

        // =========================
        // GET ITEMS
        // =========================
        var items = await _repo.GetCollectionItemsDetailAsync(collectionId);

        // =========================
        // ✅ COUNT LOGIC
        // =========================
        var problemCount = items.Count(x => x.ProblemId != null);
        var contestCount = items.Count(x => x.ContestId != null);

        var result = new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            Type = collection.Type,
            IsVisibility = collection.IsVisibility,

            // ✅ NEW
            TotalItems = items.Count,
            ProblemCount = problemCount,
            ContestCount = contestCount,

            Items = items.Select(x => new CollectionItemDto
            {
                Id = x.Id,
                ProblemId = x.ProblemId,
                ProblemTitle = x.Problem?.Title,

                ContestId = x.ContestId,
                ContestTitle = x.Contest?.Title,

                CreatedAt = x.CreatedAt
            }).ToList()
        };

        Console.WriteLine($"✅ Total: {items.Count} | Problems: {problemCount} | Contests: {contestCount}");

        return result;
    }
}