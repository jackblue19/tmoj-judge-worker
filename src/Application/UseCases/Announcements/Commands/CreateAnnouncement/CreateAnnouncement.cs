using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
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

    public CreateAnnouncementCommandHandler(
        IAnnouncementRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        var announcement = new Announcement
        {
            AnnouncementId = Guid.NewGuid(),
            AuthorId = _currentUser.UserId,
            Title = request.Title,
            Content = request.Content,
            // Lưu ngày hết hạn vào trường Target dưới dạng chuỗi ISO để so sánh
            Target = DateTime.UtcNow.AddHours(request.DurationHours).ToString("O"),
            Pinned = request.Pinned,
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(announcement);
        await _repo.SaveChangesAsync();

        return announcement.AnnouncementId;
    }
}
