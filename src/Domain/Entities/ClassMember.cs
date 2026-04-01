using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClassMember
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsActive { get; set; }

    public Guid ClassSemesterId { get; set; }

    public virtual ClassSemester ClassSemester { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
