using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[PrimaryKey("Day", "ContestId")]
[Table("contest_analytics")]
public partial class ContestAnalytic
{
    [Key]
    [Column("day")]
    public DateOnly Day { get; set; }

    [Key]
    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Column("submissions_count")]
    public int SubmissionsCount { get; set; }

    [Column("accepted_count")]
    public int AcceptedCount { get; set; }

    [Column("unique_users")]
    public int UniqueUsers { get; set; }

    [Column("unique_teams")]
    public int UniqueTeams { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ContestAnalytics")]
    public virtual Contest Contest { get; set; } = null!;
}
