using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserStudyProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid StudyPlanId { get; set; }

    public Guid ProblemId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual StudyPlan StudyPlan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
