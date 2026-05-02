using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommandHandler : IRequestHandler<BroadcastNotificationCommand, int>
{
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BroadcastNotificationCommandHandler> _logger;

    public BroadcastNotificationCommandHandler(
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        ICurrentUserService currentUser,
        ILogger<BroadcastNotificationCommandHandler> logger)
    {
        _userRepo = userRepo;
        _notificationRepo = notificationRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<int> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        try 
        {
            var adminId = _currentUser.UserId;
            
            // --- CODE THEO DB (Dòng 58) ---
            var finalType = request.Type?.ToLower() ?? "system";
            var finalScopeType = request.ScopeType?.ToLower();

            // Đã fix Check Constraint dưới DB nên không cần map lùi về system nữa

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
                    Type = finalType,
                    ScopeType = finalScopeType,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR broadcasting notification to Role={Role}", request.TargetRole);
            throw;
        }
    }
}
