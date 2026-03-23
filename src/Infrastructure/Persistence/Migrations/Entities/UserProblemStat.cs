using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[PrimaryKey("UserId", "ProblemId")]
[Table("user_problem_stats")]
public partial class UserProblemStat
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Key]
    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("attempts")]
    public int Attempts { get; set; }

    [Column("solved")]
    public bool Solved { get; set; }

    [Column("best_submission_id")]
    public Guid? BestSubmissionId { get; set; }

    [Column("last_submission_at")]
    public DateTime? LastSubmissionAt { get; set; }

    [ForeignKey("BestSubmissionId")]
    [InverseProperty("UserProblemStats")]
    public virtual Submission? BestSubmission { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("UserProblemStats")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserProblemStats")]
    public virtual User User { get; set; } = null!;
}
