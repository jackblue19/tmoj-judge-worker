using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class RatingHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ContestId { get; set; }

    public string ScopeType { get; set; } = null!;

    public Guid? ScopeId { get; set; }

    public int OldRating { get; set; }

    public int NewRating { get; set; }

    public int RatingChange { get; set; }

    public int RankInContest { get; set; }

    public decimal? Score { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
