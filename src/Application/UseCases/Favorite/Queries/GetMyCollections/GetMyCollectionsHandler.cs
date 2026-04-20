using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Queries.GetMyCollections;

public class GetMyCollectionsHandler
    : IRequestHandler<GetMyCollectionsQuery, List<CollectionDto>>
{
    private readonly IFavoriteRepository _repo;

    public GetMyCollectionsHandler(IFavoriteRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CollectionDto>> Handle(
        GetMyCollectionsQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("🔥 GetMyCollections START");

        var userId = request.UserId;

        if (userId == Guid.Empty)
        {
            Console.WriteLine("❌ UserId invalid");
            throw new Exception("UserId invalid");
        }

        Console.WriteLine($"👉 UserId: {userId}");

        var collections = await _repo.GetCollectionsByUserAsync(userId);

        if (collections == null || !collections.Any())
        {
            Console.WriteLine("👉 No collections found");
            return new List<CollectionDto>();
        }

        var result = collections.Select(x => new CollectionDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Type = x.Type,
            IsVisibility = x.IsVisibility,
            CreatedAt = x.CreatedAt
        }).ToList();

        Console.WriteLine($"✅ Found {result.Count} collections");

        Console.WriteLine("🔥 GetMyCollections END");

        return result;
    }
}