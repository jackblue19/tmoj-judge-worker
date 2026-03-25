using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ScoreRecalcJob
{
    public Guid Id { get; set; }

    public string? Scope { get; set; }

    public Guid? ContestId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime EnqueueAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? Errors { get; set; }

    public Guid? ContestProblemId { get; set; }

    public Guid? ContestEntryId { get; set; }

    public virtual Contest? Contest { get; set; }

    public virtual ContestTeam? ContestEntry { get; set; }

    public virtual ContestProblem? ContestProblem { get; set; }
}
