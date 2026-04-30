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

    public async Task<(bool EmailEnabled, bool PushEnabled)> GetNotificationSettingsAsync()
    {
        var emailEnabled = await IsEmailEnabledAsync();
        var pushEnabled = await IsPushEnabledAsync();
        return (emailEnabled, pushEnabled);
    }

    public async Task UpdateNotificationSettingsAsync(bool emailEnabled, bool pushEnabled)
    {
        await UpsertSettingAsync("email_notifications_enabled", emailEnabled.ToString().ToLower());
        await UpsertSettingAsync("push_notifications_enabled", pushEnabled.ToString().ToLower());
        
        await _db.SaveChangesAsync();
    }

    private async Task UpsertSettingAsync(string key, string value)
    {
        var setting = await _db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new Domain.Entities.GlobalSetting
            {
                Id = System.Guid.NewGuid(),
                Key = key,
                Value = value,
                Description = "System generated setting"
            };
            await _db.GlobalSettings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = System.DateTime.UtcNow;
        }
    }
}
