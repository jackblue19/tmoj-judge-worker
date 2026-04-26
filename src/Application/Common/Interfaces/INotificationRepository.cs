using Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface INotificationRepository
{
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
