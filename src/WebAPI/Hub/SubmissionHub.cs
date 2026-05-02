using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebAPI.Hubs;

[Authorize]
public sealed class SubmissionHub : Microsoft.AspNetCore.SignalR.Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            Context.User?.FindFirstValue("sub") ??
            Context.User?.FindFirstValue("user_id") ??
            Context.User?.FindFirstValue("uid");

        if ( Guid.TryParse(userId , out var uid) && uid != Guid.Empty )
        {
            await Groups.AddToGroupAsync(Context.ConnectionId , GetUserGroup(uid));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            Context.User?.FindFirstValue("sub") ??
            Context.User?.FindFirstValue("user_id") ??
            Context.User?.FindFirstValue("uid");

        if ( Guid.TryParse(userId , out var uid) && uid != Guid.Empty )
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId , GetUserGroup(uid));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GetUserGroup(Guid userId) => $"submission-user-{userId}";
}