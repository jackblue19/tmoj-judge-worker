using Application.Common.Interfaces;
using Application.UseCases.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using WebAPI.Models.Common;
using System.Threading.Tasks;

namespace WebAPI.Controllers.v1.Dashboard;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    [HttpGet("stats")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _dashboardRepository.GetDashboardStatsAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result, "Dashboard statistics fetched successfully"));
    }
}
