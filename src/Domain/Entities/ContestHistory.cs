using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestHistory
{
    public Guid HistoryId { get; set; }

    public Guid ContestId { get; set; }

    public int? Score { get; set; }

    public int? Ranking { get; set; }

    public DateTime ParticipatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
