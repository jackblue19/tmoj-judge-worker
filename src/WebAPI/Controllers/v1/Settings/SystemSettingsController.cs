using Application.Common.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers.v1.Settings;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[ApiController]
[Authorize(Roles = "admin")] // Chỉ admin mới được xem/sửa cấu hình hệ thống
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;

    public SystemSettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var (emailEnabled, pushEnabled) = await _settingsService.GetNotificationSettingsAsync();
        return Ok(new
        {
            success = true,
            data = new
            {
                emailNotifications = emailEnabled,
                pushNotifications = pushEnabled
            }
        });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsDto request)
    {
        await _settingsService.UpdateNotificationSettingsAsync(request.EmailNotifications, request.PushNotifications);
        return Ok(new
        {
            success = true,
            message = "Global notification settings updated successfully."
        });
    }
}

public class UpdateNotificationSettingsDto
{
    public bool EmailNotifications { get; set; }
    public bool PushNotifications { get; set; }
}
