using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly TmojDbContext _db;
    private readonly ILogger<AnnouncementRepository> _logger;

    public AnnouncementRepository(TmojDbContext db, ILogger<AnnouncementRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Announcement>> GetActiveAnnouncementsAsync()
    {
        try 
        {
            var nowString = DateTime.UtcNow.ToString("O");
            
            _logger.LogInformation("FETCHING announcements... nowString={nowString}", nowString);

            var all = await _db.Announcements
                .AsNoTracking()
                .OrderByDescending(a => a.Pinned)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("FOUND {count} raw announcements", all.Count);

            var filtered = all
                .Where(a => a.Target == "all" || (a.Target != null && string.Compare(a.Target, nowString) > 0))
                .Take(10)
                .ToList();

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DATABASE ERROR in GetActiveAnnouncementsAsync");
            throw;
        }
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
