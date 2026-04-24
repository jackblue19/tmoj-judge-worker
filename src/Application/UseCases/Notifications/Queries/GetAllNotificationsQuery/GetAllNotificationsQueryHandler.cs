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

namespace Application.UseCases.Notifications.Queries.GetAllNotificationsQuery;

public class GetAllNotificationsQueryHandler : IRequestHandler<GetAllNotificationsQuery, List<NotificationDto>>
{
    private readonly IReadRepository<Notification, Guid> _readRepo;

    public GetAllNotificationsQueryHandler(IReadRepository<Notification, Guid> readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<List<NotificationDto>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
    {
        var spec = new AllNotificationsSpec();
        var notifications = await _readRepo.ListAsync(spec, cancellationToken);
        
        return notifications.Select(NotificationDto.FromEntity).ToList();
    }
}
