using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("contest_teams")]
[Index("ContestId", "TeamId", Name = "contest_teams_contest_id_team_id_key", IsUnique = true)]
public partial class ContestTeam
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Column("team_id")]
    public Guid TeamId { get; set; }

    [Column("join_at")]
    public DateTime JoinAt { get; set; }

    [Column("rank")]
    public int? Rank { get; set; }

    [Column("score")]
    [Precision(18, 2)]
    public decimal? Score { get; set; }

    [Column("solved_problem")]
    public int SolvedProblem { get; set; }

    [Column("submissions_count")]
    public int SubmissionsCount { get; set; }

    [Column("penalty")]
    public int Penalty { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ContestTeams")]
    public virtual Contest Contest { get; set; } = null!;

    [InverseProperty("Entry")]
    public virtual ICollection<ContestScoreboardEntry> ContestScoreboardEntries { get; set; } = new List<ContestScoreboardEntry>();

    [InverseProperty("Entry")]
    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    [InverseProperty("ContestEntry")]
    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    [ForeignKey("TeamId")]
    [InverseProperty("ContestTeams")]
    public virtual Team Team { get; set; } = null!;
}
