using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Favorite.Commands.ReorderCollectionItems;

public class ReorderCollectionItemsHandler
    : IRequestHandler<ReorderCollectionItemsCommand, bool>
{
    private readonly IFavoriteRepository _repo;

    public ReorderCollectionItemsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> Handle(
        ReorderCollectionItemsCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 ReorderCollectionItems START");

        if (request.Items == null || !request.Items.Any())
            throw new Exception("Items list is empty");

        // =========================
        // CHECK COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(request.CollectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        if (collection.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not your collection");

        // =========================
        // LOAD DB ITEMS
        // =========================
        var dbItems = await _repo.GetCollectionItemsByCollectionId(request.CollectionId);

        var dbMap = dbItems.ToDictionary(x => x.Id);

        // =========================
        // VALIDATE COUNT
        // =========================
        if (dbItems.Count != request.Items.Count)
            throw new Exception("Items count mismatch");

        // =========================
        // VALIDATE ITEM ID
        // =========================
        foreach (var item in request.Items)
        {
            if (!dbMap.ContainsKey(item.ItemId))
                throw new Exception($"Invalid itemId: {item.ItemId}");
        }

        // =========================
        // VALIDATE DUPLICATE ORDER
        // =========================
        var hasDuplicateOrder = request.Items
            .GroupBy(x => x.OrderIndex)
            .Any(g => g.Count() > 1);

        if (hasDuplicateOrder)
            throw new Exception("Duplicate orderIndex detected");

        // =========================
        // NORMALIZE ORDER (optional nhưng nên có)
        // =========================
        var normalized = request.Items
            .OrderBy(x => x.OrderIndex)
            .Select((x, index) => new
            {
                x.ItemId,
                OrderIndex = index + 1 // đảm bảo luôn 1..N
            })
            .ToList();

        // =========================
        // APPLY ORDER
        // =========================
        foreach (var dto in normalized)
        {
            var entity = dbMap[dto.ItemId];
            entity.OrderIndex = dto.OrderIndex;
        }

        await _repo.SaveChangesAsync();

        Console.WriteLine("✅ Reorder success");

        return true;
    }
}