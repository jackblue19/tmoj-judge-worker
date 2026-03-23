using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("team")]
[Index("InviteCode", Name = "team_invite_code_key", IsUnique = true)]
public partial class Team
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("leader_id")]
    public Guid LeaderId { get; set; }

    [Column("team_size")]
    public int TeamSize { get; set; }

    [Column("team_name")]
    public string TeamName { get; set; } = null!;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("invite_code")]
    public string? InviteCode { get; set; }

    [Column("is_personal")]
    public bool IsPersonal { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Team")]
    public virtual ICollection<ContestTeam> ContestTeams { get; set; } = new List<ContestTeam>();

    [ForeignKey("LeaderId")]
    [InverseProperty("Teams")]
    public virtual User Leader { get; set; } = null!;

    [InverseProperty("Team")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    [InverseProperty("Team")]
    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
