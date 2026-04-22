using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Favorite.Commands.AddProblemToCollection;

public class AddProblemToCollectionHandler
    : IRequestHandler<AddProblemToCollectionCommand, AddProblemToCollectionResult>
{
    private readonly IFavoriteRepository _repo;

    public AddProblemToCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<AddProblemToCollectionResult> Handle(
        AddProblemToCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 AddProblemToCollection START");

        // =========================
        // CHECK COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(request.CollectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        if (collection.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not your collection");

        // =========================
        // CHECK PROBLEM
        // =========================
        var problem = await _repo.GetProblemByIdAsync(request.ProblemId);

        if (problem == null)
            throw new Exception("Problem not found");

        // =========================
        // CHECK DUPLICATE (APP LEVEL)
        // =========================
        var existed = await _repo.GetCollectionItemAsync(
            request.CollectionId,
            request.ProblemId,
            null);

        if (existed != null)
        {
            Console.WriteLine("⚠️ Already in collection");

            return new AddProblemToCollectionResult
            {
                IsSuccess = false,
                IsAlreadyExists = true,
                CollectionId = request.CollectionId,
                ProblemId = request.ProblemId,
                ItemId = existed.Id,
                Message = "Problem already exists in collection"
            };
        }

        // =========================
        // 🔥 GET ORDER INDEX (FIX CORE BUG)
        // =========================
        var nextOrderIndex = await GetNextOrderIndex(request.CollectionId, ct);

        // =========================
        // CREATE ITEM
        // =========================
        var item = new CollectionItem
        {
            Id = Guid.NewGuid(),
            CollectionId = request.CollectionId,
            ProblemId = request.ProblemId,
            ContestId = null,
            OrderIndex = nextOrderIndex, // 🔥 FIX QUAN TRỌNG
            CreatedAt = DateTime.UtcNow
        };

        // =========================
        // 🔥 SAFE INSERT (DB LEVEL)
        // =========================
        var success = await _repo.TryAddItemAsync(item);

        if (!success)
        {
            Console.WriteLine("⚠️ Duplicate caught at DB level");

            return new AddProblemToCollectionResult
            {
                IsSuccess = false,
                IsAlreadyExists = true,
                CollectionId = request.CollectionId,
                ProblemId = request.ProblemId,
                Message = "Problem already exists (DB constraint)"
            };
        }

        Console.WriteLine("✅ Added problem to collection");

        return new AddProblemToCollectionResult
        {
            IsSuccess = true,
            IsAlreadyExists = false,
            CollectionId = request.CollectionId,
            ProblemId = request.ProblemId,
            ItemId = item.Id,
            Message = "Added problem to collection successfully"
        };
    }

    // =========================
    // 🔥 HELPER: ORDER INDEX
    // =========================
    private async Task<int> GetNextOrderIndex(Guid collectionId, CancellationToken ct)
    {
        var items = await _repo
            .GetCollectionItemsByCollectionId(collectionId);

        if (items == null || items.Count == 0)
            return 1;

        return items.Max(x => x.OrderIndex) + 1;
    }
}