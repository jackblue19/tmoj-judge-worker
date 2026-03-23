using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("study_plan_items")]
public partial class StudyPlanItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("study_plan_id")]
    public Guid StudyPlanId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("StudyPlanItems")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("StudyPlanId")]
    [InverseProperty("StudyPlanItems")]
    public virtual StudyPlan StudyPlan { get; set; } = null!;
}
