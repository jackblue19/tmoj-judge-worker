using Application.Common.Interfaces;
using Application.UseCases.Favorite.Dtos;
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

    // =========================
    // GET COLLECTION (TRACKED)
    // =========================
    public async Task<Collection?> GetUserCollectionByTypeAsync(Guid userId, string type)
    {
        return await _db.Set<Collection>()
            // ❌ REMOVE AsNoTracking
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Type == type);
    }

    public async Task<Collection?> GetCollectionByIdAsync(Guid id)
    {
        return await _db.Set<Collection>()
            // ❌ REMOVE AsNoTracking
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Collection>> GetCollectionsByUserAsync(Guid userId)
    {
        return await _db.Set<Collection>()
            .AsNoTracking() // ✅ read-only
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    // =========================
    // GET ITEMS DETAIL
    // =========================
    public async Task<List<CollectionItem>> GetCollectionItemsDetailAsync(Guid collectionId)
    {
        return await _db.Set<CollectionItem>()
            .AsNoTracking()
            .Include(x => x.Problem)
            .Include(x => x.Contest)
            .Where(x => x.CollectionId == collectionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    // =========================
    // QUERY FAVORITES (READ ONLY)
    // =========================
    public IQueryable<Problem> QueryFavoriteProblems(Guid collectionId)
    {
        return _db.Set<CollectionItem>()
            .AsNoTracking()
            .Where(ci =>
                ci.CollectionId == collectionId &&
                ci.ProblemId.HasValue)
            .Join(
                _db.Set<Problem>().AsNoTracking(),
                ci => ci.ProblemId!.Value,
                p => p.Id,
                (ci, p) => p
            )
            .Distinct();
    }

    public IQueryable<Contest> QueryFavoriteContests(Guid collectionId)
    {
        return _db.Set<CollectionItem>()
            .AsNoTracking()
            .Where(ci =>
                ci.CollectionId == collectionId &&
                ci.ContestId.HasValue)
            .Join(
                _db.Set<Contest>().AsNoTracking(),
                ci => ci.ContestId!.Value,
                c => c.Id,
                (ci, c) => c
            )
            .Distinct();
    }

    // =========================
    // CHECK FAVORITE
    // =========================
    public async Task<bool> IsFavoritedAsync(
        Guid userId,
        Guid? problemId,
        Guid? contestId)
    {
        string type = problemId != null
            ? "problem_favorite"
            : "contest_favorite";

        var collection = await _db.Set<Collection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Type == type);

        if (collection == null)
            return false;

        return await _db.Set<CollectionItem>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.CollectionId == collection.Id &&
                x.ProblemId == problemId &&
                x.ContestId == contestId);
    }

    // =========================
    // VALIDATION
    // =========================
    public async Task<bool> IsCollectionNameExistsAsync(Guid userId, string name, string type)
    {
        return await _db.Set<Collection>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == userId &&
                x.Type == type &&
                x.Name.ToLower() == name.ToLower());
    }

    // =========================
    // GET ENTITY (READ ONLY)
    // =========================
    public async Task<Problem?> GetProblemByIdAsync(Guid problemId)
    {
        return await _db.Set<Problem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == problemId);
    }

    public async Task<Contest?> GetContestByIdAsync(Guid contestId)
    {
        return await _db.Set<Contest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contestId);
    }

    public async Task<CollectionItem?> GetCollectionItemAsync(
        Guid collectionId,
        Guid? problemId,
        Guid? contestId)
    {
        return await _db.Set<CollectionItem>()
            .FirstOrDefaultAsync(x =>
                x.CollectionId == collectionId &&
                x.ProblemId == problemId &&
                x.ContestId == contestId);
    }

    public async Task<List<CollectionItem>> GetCollectionItemsByCollectionId(Guid collectionId)
    {
        return await _db.Set<CollectionItem>()
            // ✅ TRACKED (important for reorder)
            .Where(x => x.CollectionId == collectionId)
            .ToListAsync();
    }

    // =========================
    // PUBLIC COLLECTIONS
    // =========================
    public IQueryable<Collection> QueryPublicCollections()
    {
        return _db.Set<Collection>()
            .AsNoTracking()
            .Where(x => x.IsVisibility == true);
    }

    public async Task<(List<PublicCollectionDto> Items, int Total)> GetPublicCollectionsAsync(
    Guid currentUserId,
    int page,
    int pageSize)
    {
        var query = _db.Set<Collection>()
            .AsNoTracking()
            .Where(x => x.IsVisibility == true);

        var total = await query.CountAsync();

        var collections = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c,
                totalItems = c.CollectionItems.Count(),

                problemCount = c.CollectionItems.Count(ci => ci.ProblemId != null),
                contestCount = c.CollectionItems.Count(ci => ci.ContestId != null),

                solvedCount =
                    (from ci in c.CollectionItems
                     where ci.ProblemId != null
                     join s in _db.Set<Submission>()
                         on ci.ProblemId equals s.ProblemId
                     where s.UserId == currentUserId
                           && !s.IsDeleted
                           && s.StatusCode == "accepted"
                     select ci.ProblemId)
                    .Distinct()
                    .Count()
            })
            .Select(x => new PublicCollectionDto
            {
                Id = x.c.Id,
                Name = x.c.Name,
                Description = x.c.Description,

                OwnerId = x.c.UserId,
                OwnerName = x.c.User.Username,

                TotalItems = x.totalItems,
                ProblemCount = x.problemCount,
                ContestCount = x.contestCount,

                SolvedCount = x.solvedCount,

                SolvedPercent = x.problemCount == 0
                    ? 0
                    : (x.solvedCount * 100.0 / x.problemCount),

                PreviewItems = x.c.CollectionItems
                    .OrderBy(ci => ci.OrderIndex)
                    .Take(3)
                    .Select(ci => new PreviewItemDto
                    {
                        ItemId = ci.Id,
                        ProblemId = ci.ProblemId,
                        ProblemTitle = ci.Problem != null ? ci.Problem.Title : null,
                        ContestId = ci.ContestId,
                        ContestTitle = ci.Contest != null ? ci.Contest.Title : null
                    }).ToList()
            })
            .ToListAsync();

        return (collections, total);
    }

    // =========================
    // CREATE
    // =========================
    public async Task CreateAsync(Collection collection)
    {
        await _db.Set<Collection>().AddAsync(collection);
    }

    public async Task AddItemAsync(CollectionItem item)
    {
        await _db.Set<CollectionItem>().AddAsync(item);
    }

    // =========================
    // DELETE
    // =========================
    public async Task RemoveItemAsync(Guid itemId)
    {
        var item = await _db.Set<CollectionItem>()
            .FirstOrDefaultAsync(x => x.Id == itemId);

        if (item != null)
            _db.Set<CollectionItem>().Remove(item);
    }

    public async Task DeleteItemsByCollectionIdAsync(Guid collectionId)
    {
        var items = await _db.Set<CollectionItem>()
            .Where(x => x.CollectionId == collectionId)
            .ToListAsync();

        _db.Set<CollectionItem>().RemoveRange(items);
    }

    public Task DeleteCollectionAsync(Collection collection)
    {
        _db.Set<Collection>().Remove(collection);
        return Task.CompletedTask;
    }

    public async Task<CollectionItem?> GetCollectionItemByIdAsync(Guid itemId)
    {
        return await _db.Set<CollectionItem>()
            .FirstOrDefaultAsync(x => x.Id == itemId);
    }

    // =========================
    // SAVE
    // =========================
    public async Task SaveChangesAsync()
    {
        var affected = await _db.SaveChangesAsync();
        _logger.LogInformation("🔥 SaveChanges affected rows: {count}", affected);
    }
    public IQueryable<Submission> QuerySubmissions()
    {
        return _db.Set<Submission>().AsNoTracking();
    }

    // =========================
    // LEGACY
    // =========================
    public async Task<(List<Problem> Items, int Total)> GetFavoriteProblemsAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        var query =
            from c in _db.Set<Collection>()
            join ci in _db.Set<CollectionItem>() on c.Id equals ci.CollectionId
            join p in _db.Set<Problem>() on ci.ProblemId equals p.Id
            where c.UserId == userId
                  && c.Type == "problem_favorite"
                  && ci.ProblemId != null
            select p;

        var total = await query.CountAsync();

        var items = await query
            .Distinct()
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<HashSet<Guid>> GetSolvedProblemIdsAsync(
     Guid userId,
     List<Guid> problemIds)
    {
        if (problemIds == null || problemIds.Count == 0)
            return new HashSet<Guid>();

        var list = await _db.Set<Submission>()
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                problemIds.Contains(x.ProblemId) &&
                x.StatusCode == "accepted")
            .Select(x => x.ProblemId)
            .Distinct()
            .ToListAsync();

        return list.ToHashSet();
    }
}