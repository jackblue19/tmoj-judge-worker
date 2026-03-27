using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClassSlotProblem
{
    public Guid SlotId { get; set; }

    public Guid ProblemId { get; set; }

    public int? Ordinal { get; set; }

    public int? Points { get; set; }

    public bool IsRequired { get; set; }

    public Guid Id { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual ClassSlot Slot { get; set; } = null!;
}
