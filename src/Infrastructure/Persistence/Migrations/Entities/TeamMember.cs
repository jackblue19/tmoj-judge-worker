using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[PrimaryKey("TeamId", "UserId")]
[Table("team_members")]
public partial class TeamMember
{
    [Key]
    [Column("team_id")]
    public Guid TeamId { get; set; }

    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [ForeignKey("TeamId")]
    [InverseProperty("TeamMembers")]
    public virtual Team Team { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("TeamMembers")]
    public virtual User User { get; set; } = null!;
}
