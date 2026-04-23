using Application.UseCases.Favorite.Dtos;
using Domain.Entities;

public interface IFavoriteRepository
{
    Task<Collection?> GetUserCollectionByTypeAsync(Guid userId, string type);

    Task<CollectionItem?> GetCollectionItemAsync(
        Guid collectionId,
        Guid? problemId,
        Guid? contestId);

    Task<bool> IsFavoritedAsync(
        Guid userId,
        Guid? problemId,
        Guid? contestId);

    Task<bool> IsCollectionNameExistsAsync(Guid userId, string name, string type);

    Task<Collection?> GetCollectionByIdAsync(Guid id);

    IQueryable<Problem> QueryFavoriteProblems(Guid collectionId);
    IQueryable<Contest> QueryFavoriteContests(Guid collectionId);
    IQueryable<Submission> QuerySubmissions();

    IQueryable<Collection> QueryPublicCollections();
    IQueryable<Collection> QueryCollections(); // ✅ Thêm hàm này để query tất cả

    Task<(List<PublicCollectionDto> Items, int Total)> GetPublicCollectionsAsync(
        Guid currentUserId,
        int page,
        int pageSize);

    Task<List<Collection>> GetCollectionsByUserAsync(Guid userId);

    Task<List<CollectionItem>> GetCollectionItemsDetailAsync(Guid collectionId);

    Task<Problem?> GetProblemByIdAsync(Guid problemId);
    Task<Contest?> GetContestByIdAsync(Guid contestId);

    Task CreateAsync(Collection collection);
    Task AddItemAsync(CollectionItem item);

    // 🔥 ADD: safe insert (handle race condition)
    Task<bool> TryAddItemAsync(CollectionItem item);

    Task RemoveItemAsync(Guid itemId);

    Task<CollectionItem?> GetCollectionItemByIdAsync(Guid itemId);

    Task<List<CollectionItem>> GetCollectionItemsByCollectionId(Guid collectionId);

    Task DeleteCollectionAsync(Collection collection);
    Task DeleteItemsByCollectionIdAsync(Guid collectionId);

    Task SaveChangesAsync();

    Task<(List<Problem> Items, int Total)> GetFavoriteProblemsAsync(
        Guid userId,
        int page,
        int pageSize);

    Task<HashSet<Guid>> GetSolvedProblemIdsAsync(
        Guid userId,
        List<Guid> problemIds);
}