using MediatR;
using System;

namespace Application.UseCases.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommand : IRequest<bool>
{
    public Guid NotificationId { get; set; }

    public MarkNotificationAsReadCommand(Guid notificationId)
    {
        NotificationId = notificationId;
    }
}
