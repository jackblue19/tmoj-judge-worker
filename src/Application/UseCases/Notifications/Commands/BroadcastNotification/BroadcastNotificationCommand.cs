using MediatR;

namespace Application.UseCases.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommand : IRequest<int>
{
    public string? TargetRole { get; set; } // null = all
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public string Type { get; set; } = "system";
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
}
