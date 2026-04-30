using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface ISystemSettingsService
{
    Task<bool> IsEmailEnabledAsync();
    Task<bool> IsPushEnabledAsync();
    Task<(bool EmailEnabled, bool PushEnabled)> GetNotificationSettingsAsync();
    Task UpdateNotificationSettingsAsync(bool emailEnabled, bool pushEnabled);
}
