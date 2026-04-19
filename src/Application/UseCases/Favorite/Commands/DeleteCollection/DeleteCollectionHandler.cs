using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Commands.DeleteCollection;

public class DeleteCollectionHandler
    : IRequestHandler<DeleteCollectionCommand, DeleteCollectionResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public DeleteCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<DeleteCollectionResponseDto> Handle(
        DeleteCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 DeleteCollection START");

        try
        {
            // =========================
            // VALIDATE
            // =========================
            if (request.Id == Guid.Empty)
                throw new Exception("CollectionId invalid");

            if (request.UserId == Guid.Empty)
                throw new Exception("UserId invalid");

            // =========================
            // GET COLLECTION
            // =========================
            var collection = await _repo.GetCollectionByIdAsync(request.Id);

            if (collection == null)
            {
                Console.WriteLine("❌ Collection not found");

                return new DeleteCollectionResponseDto
                {
                    IsSuccess = false,
                    ErrorCode = "COLLECTION_NOT_FOUND",
                    ErrorMessage = "Collection not found"
                };
            }

            // =========================
            // CHECK OWNER
            // =========================
            if (collection.UserId != request.UserId)
            {
                Console.WriteLine("❌ Forbidden delete");

                return new DeleteCollectionResponseDto
                {
                    IsSuccess = false,
                    ErrorCode = "FORBIDDEN",
                    ErrorMessage = "You are not owner of this collection"
                };
            }

            // =========================
            // DELETE ITEMS FIRST (SAFE)
            // =========================
            await _repo.DeleteItemsByCollectionIdAsync(collection.Id);

            // =========================
            // DELETE COLLECTION
            // =========================
            await _repo.DeleteCollectionAsync(collection);

            await _repo.SaveChangesAsync();

            Console.WriteLine("✅ DeleteCollection SUCCESS");

            return new DeleteCollectionResponseDto
            {
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");

            return new DeleteCollectionResponseDto
            {
                IsSuccess = false,
                ErrorCode = "INTERNAL_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }
}