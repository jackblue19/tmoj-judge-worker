using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly TmojDbContext _context;

    public NotificationRepository(TmojDbContext context)
    {
        _context = context;
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);
    }
}
