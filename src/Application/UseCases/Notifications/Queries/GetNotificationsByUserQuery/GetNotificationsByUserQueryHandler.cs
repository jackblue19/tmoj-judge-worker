using Application.Common.Interfaces;
using Application.UseCases.Notifications.Dtos;
using Application.UseCases.Notifications.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Queries.GetNotificationsByUserQuery;

public class GetNotificationsByUserQueryHandler : IRequestHandler<GetNotificationsByUserQuery, List<NotificationDto>>
{
    private readonly IReadRepository<Notification, Guid> _readRepo;

    public GetNotificationsByUserQueryHandler(IReadRepository<Notification, Guid> readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsByUserQuery request, CancellationToken cancellationToken)
    {
        var spec = new NotificationsByUserSpec(request.UserId);
        var notifications = await _readRepo.ListAsync(spec, cancellationToken);
        
        return notifications.Select(NotificationDto.FromEntity).ToList();
    }
}
