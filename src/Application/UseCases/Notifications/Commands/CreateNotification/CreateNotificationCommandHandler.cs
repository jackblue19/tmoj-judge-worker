using Application.Common.Interfaces;
using Application.UseCases.Notifications.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly IWriteRepository<Notification, Guid> _writeRepo;
    private readonly IUserRepository _userRepo;
    private readonly Application.Abstractions.Outbound.Services.IEmailService _emailService;
    private readonly ISystemSettingsService _systemSettings;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        IWriteRepository<Notification, Guid> writeRepo,
        IUserRepository userRepo,
        Application.Abstractions.Outbound.Services.IEmailService emailService,
        ISystemSettingsService systemSettings,
        IUnitOfWork uow,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _writeRepo = writeRepo;
        _userRepo = userRepo;
        _emailService = emailService;
        _systemSettings = systemSettings;
        _uow = uow;
        _logger = logger;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            // --- CODE THEO DB (Dựa trên dòng 58 bác chụp) ---
            var finalType = request.Type?.ToLower() ?? "system";
            var finalScopeType = request.ScopeType;

            // Nếu là report hoặc comment, ta đẩy sang cột ScopeType để né Check Constraint
            if (finalType == "report" || finalType == "comment")
            {
                _logger.LogInformation("Mapping '{Type}' to ScopeType to match DB pattern", finalType);
                if (string.IsNullOrEmpty(finalScopeType)) finalScopeType = finalType;
                finalType = "system";
            }

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = finalType,
                ScopeType = finalScopeType,
                ScopeId = request.ScopeId,
                CreatedBy = request.CreatedBy,
                IsRead = false,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            await _writeRepo.AddAsync(notification, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // =========================
            // 🔥 EMAIL NOTIFICATION LOGIC
            // =========================
            try 
            {
                if (await _systemSettings.IsEmailEnabledAsync())
                {
                    var user = await _userRepo.GetUserWithSettingsAsync(notification.UserId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        // Check user preference
                        bool canSend = user.UserNotificationSetting == null || user.UserNotificationSetting.ReceiveEmail;
                        
                        if (canSend)
                        {
                            _logger.LogInformation("Sending notification email to {Email}", user.Email);
                            await _emailService.SendEmailAsync(user.Email, notification.Title, notification.Message ?? string.Empty);
                        }
                    }
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send email notification to User {UserId}", notification.UserId);
            }

            return NotificationDto.FromEntity(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR creating notification for User={UserId}, Title={Title}", request.UserId, request.Title);
            throw;
        }
    }
}
