using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserRating
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ScopeType { get; set; } = null!;

    public Guid? ScopeId { get; set; }

    public int Rating { get; set; }

    public int MaxRating { get; set; }

    public string? RankTitle { get; set; }

    public int? Volatility { get; set; }

    public int? TimesPlayed { get; set; }

    public DateTime? LastCompetedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
