using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("rating_history")]
[Index("UserId", "Id", Name = "unique_user_contest_rating_history", IsUnique = true)]
public partial class RatingHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Column("scope_type")]
    public string ScopeType { get; set; } = null!;

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("old_rating")]
    public int OldRating { get; set; }

    [Column("new_rating")]
    public int NewRating { get; set; }

    [Column("rating_change")]
    public int RatingChange { get; set; }

    [Column("rank_in_contest")]
    public int RankInContest { get; set; }

    [Column("score")]
    [Precision(18, 2)]
    public decimal? Score { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("RatingHistories")]
    public virtual Contest Contest { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("RatingHistories")]
    public virtual User User { get; set; } = null!;
}
