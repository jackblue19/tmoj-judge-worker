using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Team
{
    public Guid Id { get; set; }

    public Guid LeaderId { get; set; }

    public int TeamSize { get; set; }

    public string TeamName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? InviteCode { get; set; }

    public bool IsPersonal { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ContestTeam> ContestTeams { get; set; } = new List<ContestTeam>();

    public virtual User Leader { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
