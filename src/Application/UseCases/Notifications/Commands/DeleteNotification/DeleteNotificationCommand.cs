using MediatR;
using System;

namespace Application.UseCases.Notifications.Commands.DeleteNotification;

public class DeleteNotificationCommand : IRequest<bool>
{
    public Guid NotificationId { get; set; }

    public DeleteNotificationCommand(Guid notificationId)
    {
        NotificationId = notificationId;
    }
}
