using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("badges")]
[Index("BadgeCode", Name = "badges_badge_code_key", IsUnique = true)]
public partial class Badge
{
    [Key]
    [Column("badge_id")]
    public Guid BadgeId { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("icon_url")]
    public string? IconUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("badge_code")]
    public string BadgeCode { get; set; } = null!;

    [Column("badge_category")]
    public string BadgeCategory { get; set; } = null!;

    [Column("badge_level")]
    public int BadgeLevel { get; set; }

    [Column("is_repeatable")]
    public bool IsRepeatable { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [InverseProperty("Badge")]
    public virtual ICollection<BadgeRule> BadgeRules { get; set; } = new List<BadgeRule>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("BadgeCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("UpdatedBy")]
    [InverseProperty("BadgeUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }

    [InverseProperty("Badge")]
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
