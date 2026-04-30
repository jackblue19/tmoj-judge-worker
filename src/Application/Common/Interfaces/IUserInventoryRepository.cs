using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.UseCases.Store.Queries.GetAdminOrders;
using Domain.Entities;

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
    Task<PagedResult<AdminOrderDto>> GetAdminOrdersAsync(GetAdminOrdersQuery request);
}
