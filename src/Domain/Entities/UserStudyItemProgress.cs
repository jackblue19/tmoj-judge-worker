using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class UserStudyItemProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid StudyPlanItemId { get; set; }

    public bool? IsCompleted { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual StudyPlanItem StudyPlanItem { get; set; } = null!;
}