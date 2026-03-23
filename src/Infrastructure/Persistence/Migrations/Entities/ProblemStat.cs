using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("problem_stats")]
public partial class ProblemStat
{
    [Key]
    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("submissions_count")]
    public long SubmissionsCount { get; set; }

    [Column("accepted_count")]
    public long AcceptedCount { get; set; }

    [Column("avg_time_ms")]
    public long? AvgTimeMs { get; set; }

    [Column("avg_memory_kb")]
    public long? AvgMemoryKb { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("ProblemStat")]
    public virtual Problem Problem { get; set; } = null!;
}
