using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Common.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly TmojDbContext _db;
    private readonly ILogger<FavoriteRepository> _logger;

    public FavoriteRepository(
        TmojDbContext db,
        ILogger<FavoriteRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Collection?> GetUserCollectionByTypeAsync(Guid userId, string type)
    {
        _logger.LogInformation("Get collection type {Type} of user {UserId}", type, userId);

        return await _db.Set<Collection>()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Type == type);
    }

    public async Task<CollectionItem?> GetCollectionItemAsync(
        Guid collectionId,
        Guid? problemId,
        Guid? contestId)
    {
        _logger.LogInformation("Check item in collection {CollectionId}", collectionId);

        return await _db.Set<CollectionItem>()
            .FirstOrDefaultAsync(x =>
                x.CollectionId == collectionId &&
                x.ProblemId == problemId &&
                x.ContestId == contestId);
    }

    public async Task CreateAsync(Collection collection)
    {
        _logger.LogInformation("Create collection {CollectionId}", collection.Id);

        await _db.Set<Collection>().AddAsync(collection);
    }

    public async Task AddItemAsync(CollectionItem item)
    {
        _logger.LogInformation("Add item into collection {CollectionId}", item.CollectionId);

        await _db.Set<CollectionItem>().AddAsync(item);
    }

    public async Task RemoveItemAsync(Guid itemId)
    {
        _logger.LogInformation("Remove item {ItemId}", itemId);

        var item = await _db.Set<CollectionItem>()
            .FirstOrDefaultAsync(x => x.Id == itemId);

        if (item != null)
        {
            _db.Set<CollectionItem>().Remove(item);
        }
    }

    public async Task SaveChangesAsync()
    {
        _logger.LogInformation("Saving changes...");

        await _db.SaveChangesAsync();
    }
}