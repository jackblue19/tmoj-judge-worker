using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestTeam
{
    public Guid Id { get; set; }

    public Guid ContestId { get; set; }

    public Guid TeamId { get; set; }

    public DateTime JoinAt { get; set; }

    public int? Rank { get; set; }

    public decimal? Score { get; set; }

    public int SolvedProblem { get; set; }

    public int SubmissionsCount { get; set; }

    public int Penalty { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual ICollection<ContestScoreboardEntry> ContestScoreboardEntries { get; set; } = new List<ContestScoreboardEntry>();

    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    public virtual Team Team { get; set; } = null!;
}
