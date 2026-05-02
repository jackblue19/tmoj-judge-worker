using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI.Services;

public class DashboardRealtimeWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<DashboardRealtimeWorker> _logger;

    public DashboardRealtimeWorker(
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> hubContext,
        ILogger<DashboardRealtimeWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard Realtime Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dashboardRepo = scope.ServiceProvider.GetRequiredService<IDashboardRepository>();
                    var stats = await dashboardRepo.GetDashboardStatsAsync();
                    
                    // Push to all admins in the group
                    await _hubContext.Clients.Group("AdminDashboard").SendAsync("ReceiveDashboardUpdate", stats, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing dashboard updates to SignalR.");
            }

            // Push every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
