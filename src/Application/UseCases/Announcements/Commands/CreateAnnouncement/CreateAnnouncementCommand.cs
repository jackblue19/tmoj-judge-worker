using MediatR;
using System;

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
