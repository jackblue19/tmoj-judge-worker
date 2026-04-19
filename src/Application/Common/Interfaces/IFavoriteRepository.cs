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

        Task CreateAsync(Collection collection);

        Task AddItemAsync(CollectionItem item);

        Task RemoveItemAsync(Guid itemId);

        Task SaveChangesAsync();
    }
}
