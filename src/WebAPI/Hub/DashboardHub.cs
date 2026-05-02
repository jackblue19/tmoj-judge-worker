using Microsoft.AspNetCore.SignalR;
using Application.UseCases.Dashboard.Dtos;
using System.Threading.Tasks;

namespace WebAPI.Hubs;

public class DashboardHub : Microsoft.AspNetCore.SignalR.Hub
{
    // Admins can join a specific group to receive dashboard updates
    public async Task JoinDashboardGroup()
    {
        if (Context.User != null && (Context.User.IsInRole("admin") || Context.User.IsInRole("manager")))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminDashboard");
        }
    }
}
