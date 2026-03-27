using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserStreak
{
    public Guid UserId { get; set; }

    public int? CurrentStreak { get; set; }

    public int? LongestStreak { get; set; }

    public DateOnly? LastActiveDate { get; set; }

    public virtual User User { get; set; } = null!;
}
