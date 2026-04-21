using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.CopyCollection;

public class CopyCollectionHandler
    : IRequestHandler<CopyCollectionCommand, CopyCollectionResult>
{
    private readonly IFavoriteRepository _repo;

    public CopyCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<CopyCollectionResult> Handle(
        CopyCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 CopyCollection START");

        // =========================
        // LOAD SOURCE
        // =========================
        var source = await _repo.GetCollectionByIdAsync(request.SourceCollectionId);

        if (source == null)
            throw new Exception("Collection not found");

        if (!source.IsVisibility)
            throw new Exception("Collection is not public");

        // =========================
        // LOAD ITEMS
        // =========================
        var items = await _repo.GetCollectionItemsByCollectionId(source.Id);

        // =========================
        // BUILD UNIQUE NAME 🔥
        // =========================
        string baseName = source.Name + " (Copy)";
        string newName = baseName;
        int attempt = 1;

        while (await _repo.IsCollectionNameExistsAsync(
            request.UserId,
            newName,
            source.Type))
        {
            newName = $"{baseName} {attempt++}";
        }

        // =========================
        // CREATE COLLECTION
        // =========================
        var newCollection = new Collection
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = newName,
            Description = source.Description,
            Type = source.Type,
            IsVisibility = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(newCollection);

        // =========================
        // COPY ITEMS (KEEP ORDER)
        // =========================
        foreach (var item in items.OrderBy(x => x.OrderIndex))
        {
            var newItem = new CollectionItem
            {
                Id = Guid.NewGuid(),
                CollectionId = newCollection.Id,
                ProblemId = item.ProblemId,
                ContestId = item.ContestId,
                OrderIndex = item.OrderIndex, // giữ nguyên thứ tự
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddItemAsync(newItem);
        }

        // =========================
        // SAVE ONCE (FAST)
        // =========================
        await _repo.SaveChangesAsync();

        Console.WriteLine("✅ CopyCollection DONE");

        return new CopyCollectionResult
        {
            NewCollectionId = newCollection.Id,
            TotalItems = items.Count,
            IsSuccess = true,
            Message = "Collection copied successfully"
        };
    }
}