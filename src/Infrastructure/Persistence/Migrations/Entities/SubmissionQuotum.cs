using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("submission_quota")]
[Index("UserId", "ProblemId", "Date", Name = "submission_quota_user_id_problem_id_date_key", IsUnique = true)]
public partial class SubmissionQuotum
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("count")]
    public int Count { get; set; }

    [Column("quota_limit")]
    public int QuotaLimit { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("SubmissionQuota")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("SubmissionQuota")]
    public virtual User User { get; set; } = null!;
}
