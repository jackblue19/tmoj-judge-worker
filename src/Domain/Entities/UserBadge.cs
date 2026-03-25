using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserBadge
{
    public Guid UserBadgesId { get; set; }

    public Guid BadgeId { get; set; }

    public Guid UserId { get; set; }

    public DateTime AwardedAt { get; set; }

    public string? MetaJson { get; set; }

    public string? ContextType { get; set; }

    public Guid? SourceId { get; set; }

    public virtual Badge Badge { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
