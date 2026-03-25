using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClassSlot
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public int SlotNo { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Rules { get; set; }

    public DateTime? OpenAt { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? CloseAt { get; set; }

    public string Mode { get; set; } = null!;

    public Guid? ContestId { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<ClassSlotProblem> ClassSlotProblems { get; set; } = new List<ClassSlotProblem>();

    public virtual Contest? Contest { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
