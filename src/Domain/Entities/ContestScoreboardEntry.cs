using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[PrimaryKey("ContestId", "EntryId")]
[Table("contest_scoreboard_entry")]
public partial class ContestScoreboardEntry
{
    [Key]
    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Key]
    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Column("total_score")]
    [Precision(18, 2)]
    public decimal TotalScore { get; set; }

    [Column("solved_count")]
    public int SolvedCount { get; set; }

    [Column("penalty_time")]
    public int PenaltyTime { get; set; }

    [Column("rank")]
    public int? Rank { get; set; }

    [Column("last_solve_at")]
    public DateTime? LastSolveAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ContestScoreboardEntries")]
    public virtual Contest Contest { get; set; } = null!;

    [ForeignKey("EntryId")]
    [InverseProperty("ContestScoreboardEntries")]
    public virtual ContestTeam Entry { get; set; } = null!;
}
