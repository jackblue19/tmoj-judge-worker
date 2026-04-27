using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
}
