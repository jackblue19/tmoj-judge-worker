using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClassMember
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
