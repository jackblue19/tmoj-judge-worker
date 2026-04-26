using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommandHandler : IRequestHandler<BroadcastNotificationCommand, int>
{
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly ICurrentUserService _currentUser;

    public BroadcastNotificationCommandHandler(
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        ICurrentUserService currentUser)
    {
        _userRepo = userRepo;
        _notificationRepo = notificationRepo;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;
        
        // 1. Get targets
        var userIds = await _userRepo.GetUserIdsByRoleAsync(request.TargetRole);

        if (userIds.Count == 0) return 0;

        // 2. Create notifications
        var now = DateTime.UtcNow;
        foreach (var userId in userIds)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                IsRead = false,
                CreatedAt = now,
                CreatedBy = adminId
            }, ct);
        }

        // 3. Save
        await _notificationRepo.SaveChangesAsync(ct);

        return userIds.Count;
    }
}
