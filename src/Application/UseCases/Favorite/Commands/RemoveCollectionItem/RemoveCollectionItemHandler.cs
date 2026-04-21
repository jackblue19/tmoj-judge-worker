using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Favorite.Commands.RemoveCollectionItem;

public class RemoveCollectionItemHandler
    : IRequestHandler<RemoveCollectionItemCommand, bool>
{
    private readonly IFavoriteRepository _repo;

    public RemoveCollectionItemHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> Handle(
        RemoveCollectionItemCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 RemoveCollectionItem START");

        // =========================
        // CHECK COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(request.CollectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        if (collection.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not your collection");

        // =========================
        // CHECK ITEM
        // =========================
        var item = await _repo.GetCollectionItemByIdAsync(request.ItemId);

        if (item == null)
        {
            Console.WriteLine("⚠️ Item not found");
            return false;
        }

        if (item.CollectionId != request.CollectionId)
            throw new Exception("Item does not belong to this collection");

        // =========================
        // REMOVE
        // =========================
        await _repo.RemoveItemAsync(item.Id);
        await _repo.SaveChangesAsync();

        Console.WriteLine("✅ Removed item");

        return true;
    }
}