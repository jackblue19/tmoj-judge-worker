using MediatR;

namespace Application.Common.Events;

public class DailyLoginEvent : INotification
{
    public Guid UserId { get; }

    public DailyLoginEvent(Guid userId)
    {
        UserId = userId;
    }
}