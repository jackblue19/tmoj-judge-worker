using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TeamMember
{
    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
