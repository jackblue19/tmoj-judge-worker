using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("contest_problems")]
[Index("ContestId", "ProblemId", Name = "contest_problems_contest_id_problem_id_key", IsUnique = true)]
public partial class ContestProblem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("ordinal")]
    public int? Ordinal { get; set; }

    [Column("alias")]
    public string? Alias { get; set; }

    [Column("display_index")]
    public int? DisplayIndex { get; set; }

    [Column("max_score")]
    public int? MaxScore { get; set; }

    [Column("override_testset_id")]
    public Guid? OverrideTestsetId { get; set; }

    [Column("time_limit_ms")]
    public int? TimeLimitMs { get; set; }

    [Column("memory_limit_kb")]
    public int? MemoryLimitKb { get; set; }

    [Column("penalty_per_wrong")]
    public int? PenaltyPerWrong { get; set; }

    [Column("scoring_code")]
    public string? ScoringCode { get; set; }

    [Column("output_limit_kb")]
    public int? OutputLimitKb { get; set; }

    [Column("points")]
    public int? Points { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ContestProblems")]
    public virtual Contest Contest { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("ContestProblems")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("OverrideTestsetId")]
    [InverseProperty("ContestProblems")]
    public virtual Testset? OverrideTestset { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("ContestProblems")]
    public virtual Problem Problem { get; set; } = null!;

    [InverseProperty("ContestProblem")]
    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    [InverseProperty("ContestProblem")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
