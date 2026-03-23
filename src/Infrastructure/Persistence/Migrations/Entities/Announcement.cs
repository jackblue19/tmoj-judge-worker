using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("announcements")]
public partial class Announcement
{
    [Key]
    [Column("announcement_id")]
    public Guid AnnouncementId { get; set; }

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("target")]
    public string Target { get; set; } = null!;

    [Column("pinned")]
    public bool Pinned { get; set; }

    [Column("scope_type")]
    public string? ScopeType { get; set; }

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("Announcements")]
    public virtual User? Author { get; set; }
}
