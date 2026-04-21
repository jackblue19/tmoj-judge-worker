using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.AddContestToCollection;

public class AddContestToCollectionHandler
    : IRequestHandler<AddContestToCollectionCommand, AddContestToCollectionResult>
{
    private readonly IFavoriteRepository _repo;

    public AddContestToCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<AddContestToCollectionResult> Handle(
        AddContestToCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 AddContestToCollection START");

        // =========================
        // CHECK COLLECTION
        // =========================
        var collection = await _repo.GetCollectionByIdAsync(request.CollectionId);

        if (collection == null)
            throw new Exception("Collection not found");

        if (collection.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not your collection");

        // =========================
        // CHECK CONTEST
        // =========================
        var contest = await _repo.GetContestByIdAsync(request.ContestId);

        if (contest == null)
            throw new Exception("Contest not found");

        // =========================
        // CHECK DUPLICATE
        // =========================
        var existed = await _repo.GetCollectionItemAsync(
            request.CollectionId,
            null,
            request.ContestId);

        if (existed != null)
        {
            Console.WriteLine("⚠️ Contest already exists in collection");

            return new AddContestToCollectionResult
            {
                IsSuccess = false,
                IsAlreadyExists = true,
                CollectionId = request.CollectionId,
                ContestId = request.ContestId,
                ItemId = existed.Id,
                Message = "Contest already exists in collection"
            };
        }

        // =========================
        // CREATE ITEM
        // =========================
        var item = new CollectionItem
        {
            Id = Guid.NewGuid(),
            CollectionId = request.CollectionId,
            ProblemId = null,
            ContestId = request.ContestId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddItemAsync(item);
        await _repo.SaveChangesAsync();

        Console.WriteLine("✅ Added contest to collection");

        return new AddContestToCollectionResult
        {
            IsSuccess = true,
            IsAlreadyExists = false,
            CollectionId = request.CollectionId,
            ContestId = request.ContestId,
            ItemId = item.Id,
            Message = "Added contest to collection successfully"
        };
    }
}