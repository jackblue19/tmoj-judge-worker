using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("user_badges")]
[Index("UserId", "BadgeId", Name = "unique_user_badge", IsUnique = true)]
public partial class UserBadge
{
    [Key]
    [Column("user_badges_id")]
    public Guid UserBadgesId { get; set; }

    [Column("badge_id")]
    public Guid BadgeId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("awarded_at")]
    public DateTime AwardedAt { get; set; }

    [Column("meta_json", TypeName = "jsonb")]
    public string? MetaJson { get; set; }

    [Column("context_type")]
    public string? ContextType { get; set; }

    [Column("source_id")]
    public Guid? SourceId { get; set; }

    [ForeignKey("BadgeId")]
    [InverseProperty("UserBadges")]
    public virtual Badge Badge { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserBadges")]
    public virtual User User { get; set; } = null!;
}
