using Application.Common.Interfaces;
using MediatR;
using Domain.Entities;

namespace Application.UseCases.Favorite.Commands.ToggleFavoriteProblem;

public class ToggleFavoriteProblemHandler
    : IRequestHandler<ToggleFavoriteProblemCommand, bool>
{
    private readonly IFavoriteRepository _repo;

    public ToggleFavoriteProblemHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> Handle(ToggleFavoriteProblemCommand request, CancellationToken ct)
    {
        Console.WriteLine("🔥 ToggleFavorite START");

        // =========================
        // VALIDATE
        // =========================
        var userId = request.UserId;
        var problemId = request.ProblemId;

        if (userId == Guid.Empty)
            throw new Exception("UserId invalid");

        if (problemId == Guid.Empty)
            throw new Exception("ProblemId invalid");

        // =========================
        // GET COLLECTION
        // =========================
        var collection = await _repo.GetUserCollectionByTypeAsync(userId, "problem_favorite");

        if (collection == null)
        {
            Console.WriteLine("👉 Collection NOT FOUND -> CREATE");

            collection = new Collection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "problem_favorite",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(collection);
            await _repo.SaveChangesAsync();
        }

        Console.WriteLine($"👉 CollectionId: {collection.Id}");

        // =========================
        // CHECK ITEM
        // =========================
        var item = await _repo.GetCollectionItemAsync(
            collection.Id,
            problemId,
            null
        );

        if (item != null)
        {
            Console.WriteLine("👉 Item EXISTS -> REMOVE");

            await _repo.RemoveItemAsync(item.Id);
            await _repo.SaveChangesAsync();

            Console.WriteLine("🔥 ToggleFavorite END (REMOVED)");

            return false;
        }

        // =========================
        // ADD ITEM
        // =========================
        Console.WriteLine("👉 Item NOT FOUND -> ADD");

        var newItem = new CollectionItem
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            ProblemId = problemId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddItemAsync(newItem);
        await _repo.SaveChangesAsync();

        Console.WriteLine("🔥 ToggleFavorite END (ADDED)");

        return true;
    }
}