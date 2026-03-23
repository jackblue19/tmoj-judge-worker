using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("score_recalc_jobs")]
public partial class ScoreRecalcJob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("scope")]
    public string? Scope { get; set; }

    [Column("contest_id")]
    public Guid? ContestId { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("enqueue_at")]
    public DateTime EnqueueAt { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("errors", TypeName = "jsonb")]
    public string? Errors { get; set; }

    [Column("contest_problem_id")]
    public Guid? ContestProblemId { get; set; }

    [Column("contest_entry_id")]
    public Guid? ContestEntryId { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ScoreRecalcJobs")]
    public virtual Contest? Contest { get; set; }

    [ForeignKey("ContestEntryId")]
    [InverseProperty("ScoreRecalcJobs")]
    public virtual ContestTeam? ContestEntry { get; set; }

    [ForeignKey("ContestProblemId")]
    [InverseProperty("ScoreRecalcJobs")]
    public virtual ContestProblem? ContestProblem { get; set; }
}
