using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("class_slot_problems")]
[Index("SlotId", "ProblemId", Name = "uq_slot_problem", IsUnique = true)]
public partial class ClassSlotProblem
{
    [Column("slot_id")]
    public Guid SlotId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("ordinal")]
    public int? Ordinal { get; set; }

    [Column("points")]
    public int? Points { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("ClassSlotProblems")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("SlotId")]
    [InverseProperty("ClassSlotProblems")]
    public virtual ClassSlot Slot { get; set; } = null!;
}
