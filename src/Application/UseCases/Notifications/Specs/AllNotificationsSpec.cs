using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Notifications.Specs;

public class AllNotificationsSpec : Specification<Notification>
{
    public AllNotificationsSpec()
    {
        Query.OrderByDescending(x => x.CreatedAt);
    }
}
