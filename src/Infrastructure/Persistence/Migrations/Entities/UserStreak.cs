using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("user_streaks")]
public partial class UserStreak
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("current_streak")]
    public int? CurrentStreak { get; set; }

    [Column("longest_streak")]
    public int? LongestStreak { get; set; }

    [Column("last_active_date")]
    public DateOnly? LastActiveDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserStreak")]
    public virtual User User { get; set; } = null!;
}
