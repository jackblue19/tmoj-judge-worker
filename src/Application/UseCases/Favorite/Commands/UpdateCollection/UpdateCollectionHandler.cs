using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.UpdateCollection;

public class UpdateCollectionHandler
    : IRequestHandler<UpdateCollectionCommand, UpdateCollectionResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public UpdateCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<UpdateCollectionResponseDto> Handle(
        UpdateCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 UpdateCollection START");

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

                return new UpdateCollectionResponseDto
                {
                    Id = request.Id,
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
                Console.WriteLine("❌ Forbidden update");

                return new UpdateCollectionResponseDto
                {
                    Id = request.Id,
                    IsSuccess = false,
                    ErrorCode = "FORBIDDEN",
                    ErrorMessage = "You are not owner of this collection"
                };
            }

            // =========================
            // UPDATE
            // =========================
            collection.Name = request.Name;
            collection.Description = request.Description;
            collection.IsVisibility = request.IsVisibility;
            collection.UpdatedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync();

            Console.WriteLine("✅ UpdateCollection SUCCESS");

            return new UpdateCollectionResponseDto
            {
                Id = collection.Id,
                Name = collection.Name,
                Description = collection.Description,
                IsVisibility = collection.IsVisibility,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");

            return new UpdateCollectionResponseDto
            {
                Id = request.Id,
                IsSuccess = false,
                ErrorCode = "INTERNAL_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }
}