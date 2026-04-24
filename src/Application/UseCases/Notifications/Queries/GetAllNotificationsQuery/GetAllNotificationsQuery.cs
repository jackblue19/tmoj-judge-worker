using Application.UseCases.Notifications.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Application.UseCases.Notifications.Queries.GetAllNotificationsQuery;

public class GetAllNotificationsQuery : IRequest<List<NotificationDto>>
{
}
