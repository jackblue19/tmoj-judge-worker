using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestScoreboardEntry
{
    public Guid ContestId { get; set; }

    public Guid EntryId { get; set; }

    public decimal TotalScore { get; set; }

    public int SolvedCount { get; set; }

    public int PenaltyTime { get; set; }

    public int? Rank { get; set; }

    public DateTime? LastSolveAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual ContestTeam Entry { get; set; } = null!;
}
