using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("contest_history")]
public partial class ContestHistory
{
    [Key]
    [Column("history_id")]
    public Guid HistoryId { get; set; }

    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Column("score")]
    public int? Score { get; set; }

    [Column("ranking")]
    public int? Ranking { get; set; }

    [Column("participated_at")]
    public DateTime ParticipatedAt { get; set; }

    [ForeignKey("HistoryId")]
    [InverseProperty("Histories")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
