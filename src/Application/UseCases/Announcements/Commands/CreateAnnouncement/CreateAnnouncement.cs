using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementCommand : IRequest<Guid>
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int DurationHours { get; set; } = 72; // Mặc định 3 ngày
    public bool Pinned { get; set; }
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
}

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IAnnouncementRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateAnnouncementCommandHandler> _logger;

    public CreateAnnouncementCommandHandler(
        IAnnouncementRepository repo,
        ICurrentUserService currentUser,
        ILogger<CreateAnnouncementCommandHandler> logger)
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
            if (userId == Guid.Empty) userId = Guid.Parse("10919393-ba32-4a23-854e-8c6a0a7ef43b"); // Dự phòng ID admin từ ảnh bác chụp

            var announcement = new Announcement
            {
                AnnouncementId = Guid.NewGuid(),
                AuthorId = userId,
                Title = request.Title,
                Content = request.Content,
                // Lưu ngày hết hạn vào trường Target
                Target = DateTime.UtcNow.AddHours(request.DurationHours == 0 ? 72 : request.DurationHours).ToString("O"),
                Pinned = request.Pinned,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
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
