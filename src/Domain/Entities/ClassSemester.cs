using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClassSemester
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid SemesterId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid? TeacherId { get; set; }

    public string? InviteCode { get; set; }

    public DateTime? InviteCodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    public virtual Semester Semester { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual User? Teacher { get; set; }
}
