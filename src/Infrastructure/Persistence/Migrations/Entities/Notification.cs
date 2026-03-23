using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("notifications")]
public partial class Notification
{
    [Key]
    [Column("notification_id")]
    public Guid NotificationId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("message")]
    public string? Message { get; set; }

    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("scope_type")]
    public string? ScopeType { get; set; }

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("NotificationCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("NotificationUsers")]
    public virtual User User { get; set; } = null!;
}
