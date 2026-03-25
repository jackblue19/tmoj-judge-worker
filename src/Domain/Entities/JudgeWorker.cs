using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class JudgeWorker
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Capabilities { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public string? Status { get; set; }

    public string? Version { get; set; }

    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();
}
