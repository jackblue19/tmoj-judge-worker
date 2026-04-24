using Ardalis.Specification;
using Domain.Entities;
using System;

namespace Application.UseCases.Notifications.Specs;

public class NotificationsByUserSpec : Specification<Notification>
{
    public NotificationsByUserSpec(Guid userId)
    {
        Query.Where(x => x.UserId == userId)
             .OrderByDescending(x => x.CreatedAt);
    }
}
