using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class StudyPlanItem
{
    public Guid Id { get; set; }

    public Guid StudyPlanId { get; set; }

    public Guid ProblemId { get; set; }

    public int OrderIndex { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual StudyPlan StudyPlan { get; set; } = null!;
}
