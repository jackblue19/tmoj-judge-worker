using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[PrimaryKey("ContestId", "EntryId", "ProblemId")]
[Table("contest_scoreboard")]
public partial class ContestScoreboard
{
    [Key]
    [Column("contest_id")]
    public Guid ContestId { get; set; }

    [Key]
    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Key]
    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("acm_solved")]
    public bool AcmSolved { get; set; }

    [Column("acm_attempts")]
    public int AcmAttempts { get; set; }

    [Column("acm_penalty_time")]
    public int AcmPenaltyTime { get; set; }

    [Column("first_ac_at")]
    public DateTime? FirstAcAt { get; set; }

    [Column("best_score")]
    [Precision(18, 2)]
    public decimal BestScore { get; set; }

    [Column("last_score")]
    [Precision(18, 2)]
    public decimal LastScore { get; set; }

    [Column("best_submission_id")]
    public Guid? BestSubmissionId { get; set; }

    [Column("last_submission_id")]
    public Guid? LastSubmissionId { get; set; }

    [Column("last_submit_at")]
    public DateTime? LastSubmitAt { get; set; }

    [Column("visible_until")]
    public DateTime? VisibleUntil { get; set; }

    [ForeignKey("BestSubmissionId")]
    [InverseProperty("ContestScoreboardBestSubmissions")]
    public virtual Submission? BestSubmission { get; set; }

    [ForeignKey("ContestId")]
    [InverseProperty("ContestScoreboards")]
    public virtual Contest Contest { get; set; } = null!;

    [ForeignKey("EntryId")]
    [InverseProperty("ContestScoreboards")]
    public virtual ContestTeam Entry { get; set; } = null!;

    [ForeignKey("LastSubmissionId")]
    [InverseProperty("ContestScoreboardLastSubmissions")]
    public virtual Submission? LastSubmission { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("ContestScoreboards")]
    public virtual Problem Problem { get; set; } = null!;
}
