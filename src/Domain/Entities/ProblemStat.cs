using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ProblemStat
{
    public Guid ProblemId { get; set; }

    public long SubmissionsCount { get; set; }

    public long AcceptedCount { get; set; }

    public long? AvgTimeMs { get; set; }

    public long? AvgMemoryKb { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Problem Problem { get; set; } = null!;
}
