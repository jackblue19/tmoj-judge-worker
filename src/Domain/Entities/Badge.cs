using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Badge
{
    public Guid BadgeId { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public string? Description { get; set; }

    public string BadgeCode { get; set; } = null!;

    public string BadgeCategory { get; set; } = null!;

    public int BadgeLevel { get; set; }

    public bool IsRepeatable { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<BadgeRule> BadgeRules { get; set; } = new List<BadgeRule>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
