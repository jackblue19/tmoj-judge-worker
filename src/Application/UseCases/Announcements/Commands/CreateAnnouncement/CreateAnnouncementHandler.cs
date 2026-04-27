using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IAnnouncementRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateAnnouncementHandler> _logger;

    public CreateAnnouncementHandler(
        IAnnouncementRepository repo,
        ICurrentUserService currentUser,
        ILogger<CreateAnnouncementHandler> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
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
                ExpiresAt = DateTime.UtcNow.AddHours(request.DurationHours == 0 ? 72 : request.DurationHours),
                Pinned = request.Pinned,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            await _repo.AddAsync(announcement);
            await _repo.SaveChangesAsync();

            return announcement.AnnouncementId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR creating announcement: {Title}", request.Title);
            throw;
        }
    }
}
