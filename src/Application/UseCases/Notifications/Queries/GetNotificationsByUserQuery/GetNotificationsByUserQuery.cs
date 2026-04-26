using Application.UseCases.Notifications.Dtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace Application.UseCases.Notifications.Queries.GetNotificationsByUserQuery;

public class GetNotificationsByUserQuery : IRequest<List<NotificationDto>>
{
    public Guid UserId { get; set; }

    public GetNotificationsByUserQuery(Guid userId)
    {
        UserId = userId;
    }
}
