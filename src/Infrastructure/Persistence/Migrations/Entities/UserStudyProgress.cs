using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("user_study_progress")]
public partial class UserStudyProgress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("study_plan_id")]
    public Guid StudyPlanId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("is_completed")]
    public bool? IsCompleted { get; set; }

    [Column("completed_at", TypeName = "timestamp without time zone")]
    public DateTime? CompletedAt { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("UserStudyProgresses")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("StudyPlanId")]
    [InverseProperty("UserStudyProgresses")]
    public virtual StudyPlan StudyPlan { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserStudyProgresses")]
    public virtual User User { get; set; } = null!;
}
