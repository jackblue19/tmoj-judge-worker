using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("user_ratings")]
[Index("UserId", "ScopeType", "ScopeId", Name = "unique_user_rating_scope", IsUnique = true)]
public partial class UserRating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("scope_type")]
    public string ScopeType { get; set; } = null!;

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("max_rating")]
    public int MaxRating { get; set; }

    [Column("rank_title")]
    public string? RankTitle { get; set; }

    [Column("volatility")]
    public int? Volatility { get; set; }

    [Column("times_played")]
    public int? TimesPlayed { get; set; }

    [Column("last_competed_at")]
    public DateTime? LastCompetedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserRatings")]
    public virtual User User { get; set; } = null!;
}
