using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class FptItemRepository : IFptItemRepository
{
    private readonly TmojDbContext _context;

    public FptItemRepository(TmojDbContext context)
    {
        _context = context;
    }

    public async Task<FptItem?> GetByIdAsync(Guid id)
    {
        return await _context.FptItems.FirstOrDefaultAsync(x => x.ItemId == id);
    }

    public async Task<List<FptItem>> GetAllActiveAsync()
    {
        return await _context.FptItems
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(FptItem entity)
    {
        await _context.FptItems.AddAsync(entity);
    }

    public async Task UpdateAsync(FptItem entity)
    {
        _context.FptItems.Update(entity);
        await Task.CompletedTask;
    }
}
