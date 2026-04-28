using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class CartItemRepository : ICartItemRepository
{
    private readonly TmojDbContext _context;

    public CartItemRepository(TmojDbContext context)
    {
        _context = context;
    }

    public async Task<CartItem?> GetByUserAndItemAsync(Guid userId, Guid itemId)
    {
        return await _context.CartItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);
    }

    public async Task<List<CartItem>> GetByUserIdAsync(Guid userId)
    {
        return await _context.CartItems
            .Include(x => x.Item)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync();
    }

    public async Task AddAsync(CartItem entity)
    {
        await _context.CartItems.AddAsync(entity);
    }

    public void Update(CartItem entity)
    {
        _context.CartItems.Update(entity);
    }

    public void Remove(CartItem entity)
    {
        _context.CartItems.Remove(entity);
    }

    public void RemoveRange(IEnumerable<CartItem> entities)
    {
        _context.CartItems.RemoveRange(entities);
    }
}
