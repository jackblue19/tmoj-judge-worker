using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestAnalytic
{
    public DateOnly Day { get; set; }

    public Guid ContestId { get; set; }

    public int SubmissionsCount { get; set; }

    public int AcceptedCount { get; set; }

    public int UniqueUsers { get; set; }

    public int UniqueTeams { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Contest Contest { get; set; } = null!;
}
