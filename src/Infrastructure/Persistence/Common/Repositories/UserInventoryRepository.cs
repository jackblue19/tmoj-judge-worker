using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.UseCases.Store.Queries.GetAdminOrders;

namespace Infrastructure.Persistence.Common.Repositories;

public class UserInventoryRepository : IUserInventoryRepository
{
    private readonly TmojDbContext _context;

    public UserInventoryRepository(TmojDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserInventory entity)
    {
        await _context.UserInventories.AddAsync(entity);
    }

    public async Task<List<UserInventory>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserInventories
            .Include(x => x.Item)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AcquiredAt)
            .ToListAsync();
    }

    public async Task<UserInventory?> GetByIdAsync(Guid inventoryId)
    {
        return await _context.UserInventories
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.InventoryId == inventoryId);
    }

    public async Task<UserInventory?> GetByUserAndItemAsync(Guid userId, Guid itemId)
    {
        return await _context.UserInventories
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);
    }

    public async Task UpdateAsync(UserInventory entity)
    {
        _context.UserInventories.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(UserInventory entity)
    {
        _context.UserInventories.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<List<UserInventory>> GetEquippedItemsByTypeAsync(Guid userId, string itemType)
    {
        return await _context.UserInventories
            .Include(x => x.Item)
            .Where(x => x.UserId == userId && x.IsEquipped && x.Item.ItemType == itemType)
            .ToListAsync();
    }

    public async Task<PagedResult<AdminOrderDto>> GetAdminOrdersAsync(GetAdminOrdersQuery request)
    {
        var query = _context.UserInventories
            .Include(x => x.User)
            .Include(x => x.Item)
            .Include(x => x.Transaction)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(x => 
                (x.User.DisplayName != null && x.User.DisplayName.ToLower().Contains(search)) ||
                (x.User.FirstName != null && x.User.FirstName.ToLower().Contains(search)) ||
                (x.User.LastName != null && x.User.LastName.ToLower().Contains(search)) ||
                x.Item.Name.ToLower().Contains(search)
            );
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            var status = request.Status.ToLower();
            query = query.Where(x => x.Transaction != null && x.Transaction.Status.ToLower() == status);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.AcquiredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AdminOrderDto
            {
                Id = x.InventoryId.ToString(), // Or TransactionId
                ItemName = x.Item.Name,
                ItemImage = x.Item.ImageUrl,
                BuyerName = x.User.DisplayName ?? (x.User.FirstName + " " + x.User.LastName) ?? x.User.Username ?? x.User.Email!,
                BuyerEmail = x.User.Email!,
                Price = x.Transaction != null ? x.Transaction.Amount : x.Item.PriceCoin,
                PurchaseDate = x.AcquiredAt,
                Status = x.Transaction != null ? x.Transaction.Status : "Completed"
            })
            .ToListAsync();

        return new PagedResult<AdminOrderDto>
        {
            Items = items,
            Total = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
