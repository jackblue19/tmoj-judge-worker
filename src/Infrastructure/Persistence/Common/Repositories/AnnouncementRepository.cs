using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly TmojDbContext _db;

    public AnnouncementRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<List<Announcement>> GetActiveAnnouncementsAsync()
    {
        // Lấy các tin mới nhất, ưu tiên tin được Ghim (Pinned)
        return await _db.Announcements
            .OrderByDescending(a => a.Pinned)
            .ThenByDescending(a => a.CreatedAt)
            .Take(10) // Chỉ lấy 10 tin mới nhất cho News Feed
            .ToListAsync();
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        return await _db.Announcements.FindAsync(id);
    }

    public async Task AddAsync(Announcement entity)
    {
        await _db.Announcements.AddAsync(entity);
    }

    public void Update(Announcement entity)
    {
        _db.Announcements.Update(entity);
    }

    public void Delete(Announcement entity)
    {
        _db.Announcements.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
