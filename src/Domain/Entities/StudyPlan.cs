using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class StudyPlan
{
    public Guid Id { get; set; }

    public Guid CreatorId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<StudyPlanItem> StudyPlanItems { get; set; } = new List<StudyPlanItem>();

    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();
}
