using System;
using System.Collections.Generic;

Domain.Entities

public partial class Announcement
{
    public Guid AnnouncementId { get; set; }

    public Guid? AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Target { get; set; } = null!;

    public bool Pinned { get; set; }

    public string? ScopeType { get; set; }

    public Guid? ScopeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? Author { get; set; }
}
