using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using Application.UseCases.Favorite.Commands.CreateCollection;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Favorite.Commands.CreateCollection;

public class CreateCollectionHandler
    : IRequestHandler<CreateCollectionCommand, CreateCollectionResponseDto>
{
    private readonly IFavoriteRepository _repo;

    public CreateCollectionHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<CreateCollectionResponseDto> Handle(
        CreateCollectionCommand request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 CreateCollection START");

        var userId = request.UserId;

        // =========================
        // VALIDATE
        // =========================
        if (userId == Guid.Empty)
            return Fail("INVALID_USER", "UserId invalid");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail("INVALID_NAME", "Collection name is required");

        if (request.Name.Length > 100)
            return Fail("NAME_TOO_LONG", "Name max length is 100");

        if (string.IsNullOrWhiteSpace(request.Type))
            return Fail("INVALID_TYPE", "Collection type is required");

        var type = request.Type.Trim().ToLower();

        // =========================
        // BUSINESS RULE
        // =========================
        if (type == "problem_favorite" || type == "contest_favorite")
        {
            return Fail("RESERVED_TYPE",
                "Cannot manually create system collection");
        }

        // =========================
        // CHECK DUPLICATE
        // =========================
        var isExist = await _repo.IsCollectionNameExistsAsync(
            userId,
            request.Name,
            type
        );

        if (isExist)
        {
            Console.WriteLine("❌ Duplicate collection name");

            return Fail("DUPLICATE_NAME",
                "Collection name already exists");
        }

        // =========================
        // CREATE
        // =========================
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Type = type,
            IsVisibility = request.IsVisibility,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(collection);
        await _repo.SaveChangesAsync();

        Console.WriteLine($"✅ Created Collection {collection.Id}");

        return new CreateCollectionResponseDto
        {
            IsSuccess = true,
            CollectionId = collection.Id,
            Name = collection.Name,
            Type = collection.Type,
            IsVisibility = collection.IsVisibility
        };
    }

    // =========================
    // HELPER
    // =========================
    private CreateCollectionResponseDto Fail(string code, string message)
    {
        return new CreateCollectionResponseDto
        {
            IsSuccess = false,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}