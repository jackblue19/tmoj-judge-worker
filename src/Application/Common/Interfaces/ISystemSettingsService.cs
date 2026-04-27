using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface ISystemSettingsService
{
    Task<bool> IsEmailEnabledAsync();
    Task<bool> IsPushEnabledAsync();
}
