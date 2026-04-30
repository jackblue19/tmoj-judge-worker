using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Application.UseCases.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IAnnouncementRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreateAnnouncementHandler> _logger;

    public CreateAnnouncementHandler(
        IAnnouncementRepository repo,
        ICurrentUserService currentUser,
        IServiceScopeFactory scopeFactory,
        ILogger<CreateAnnouncementHandler> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        try 
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty) 
            {
                throw new UnauthorizedAccessException("Bạn cần đăng nhập với quyền Admin để tạo tin tức.");
            }

            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                AuthorId = userId,
                Title = request.Title,
                Content = request.Content,
                Target = "all", // Tránh lỗi CHECK Constraint
                ExpiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(request.DurationHours == 0 ? 72 : request.DurationHours), DateTimeKind.Unspecified),
                Pinned = request.Pinned,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(announcement);
            await _repo.SaveChangesAsync();

            // Background task for sending emails
            if (request.SendEmail)
            {
                _logger.LogInformation("Broadcasting announcement via email (background task)...");
                _ = Task.Run(async () => 
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var emailService = scope.ServiceProvider.GetRequiredService<Application.Abstractions.Outbound.Services.IEmailService>();
                        var systemSettings = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();

                        if (await systemSettings.IsEmailEnabledAsync())
                        {
                            var activeUsers = await userRepo.GetAllActiveUsersWithSettingsAsync();
                            var targetUsers = activeUsers.Where(u => 
                                u.UserNotificationSetting == null || 
                                u.UserNotificationSetting.ReceiveEmail).ToList();

                            foreach (var user in targetUsers)
                            {
                                try
                                {
                                    await emailService.SendEmailAsync(user.Email!, request.Title, request.Content);
                                }
                                catch (Exception emailEx)
                                {
                                    _logger.LogError(emailEx, "Failed to send broadcast email to {Email}", user.Email);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process background broadcast emails.");
                    }
                });
            }

            return announcement.AnnouncementId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR creating announcement: {Title}", request.Title);
            throw;
        }
    }
}
