using Application.UseCases.Favorite.Dtos;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
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

        IQueryable<Collection> QueryPublicCollections();

        Task<(List<PublicCollectionDto> Items, int Total)> GetPublicCollectionsAsync(
            int page,
            int pageSize);

        IQueryable<Contest> QueryFavoriteContests(Guid collectionId);

        Task<List<Collection>> GetCollectionsByUserAsync(Guid userId);

        Task<List<CollectionItem>> GetCollectionItemsDetailAsync(Guid collectionId);

        Task<Problem?> GetProblemByIdAsync(Guid problemId);
        Task<Contest?> GetContestByIdAsync(Guid contestId);

        Task CreateAsync(Collection collection);

        Task AddItemAsync(CollectionItem item);

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
    }
}
