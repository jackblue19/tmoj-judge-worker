using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("study_plans")]
public partial class StudyPlan
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("creator_id")]
    public Guid CreatorId { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_public")]
    public bool? IsPublic { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CreatorId")]
    [InverseProperty("StudyPlans")]
    public virtual User Creator { get; set; } = null!;

    [InverseProperty("StudyPlan")]
    public virtual ICollection<StudyPlanItem> StudyPlanItems { get; set; } = new List<StudyPlanItem>();

    [InverseProperty("StudyPlan")]
    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();
}
