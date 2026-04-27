using Application.Common.Interfaces;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly TmojDbContext _db;

    public SystemSettingsService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsEmailEnabledAsync()
    {
        var setting = await _db.GlobalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "email_notifications_enabled");
            
        return setting?.Value?.ToLower() == "true";
    }

    public async Task<bool> IsPushEnabledAsync()
    {
        var setting = await _db.GlobalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "push_notifications_enabled");
            
        return setting?.Value?.ToLower() == "true";
    }
}
