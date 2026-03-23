using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("class_slot")]
[Index("ClassId", "SlotNo", Name = "ux_class_slot", IsUnique = true)]
public partial class ClassSlot
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("slot_no")]
    public int SlotNo { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("rules")]
    public string? Rules { get; set; }

    [Column("open_at")]
    public DateTime? OpenAt { get; set; }

    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    [Column("close_at")]
    public DateTime? CloseAt { get; set; }

    [Column("mode")]
    public string Mode { get; set; } = null!;

    [Column("contest_id")]
    public Guid? ContestId { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("ClassSlots")]
    public virtual Class Class { get; set; } = null!;

    [InverseProperty("Slot")]
    public virtual ICollection<ClassSlotProblem> ClassSlotProblems { get; set; } = new List<ClassSlotProblem>();

    [ForeignKey("ContestId")]
    [InverseProperty("ClassSlots")]
    public virtual Contest? Contest { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("ClassSlotCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("UpdatedBy")]
    [InverseProperty("ClassSlotUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }
}
