using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IUserInventoryRepository
{
    Task AddAsync(UserInventory entity);
    Task<List<UserInventory>> GetByUserIdAsync(Guid userId);
    Task<UserInventory?> GetByIdAsync(Guid inventoryId);
    Task UpdateAsync(UserInventory entity);
    Task<UserInventory?> GetByUserAndItemAsync(Guid userId, Guid itemId);
    Task DeleteAsync(UserInventory entity);
    Task<List<UserInventory>> GetEquippedItemsByTypeAsync(Guid userId, string itemType);
}
