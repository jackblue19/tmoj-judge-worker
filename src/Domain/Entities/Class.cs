using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Class
{
    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid SemesterId { get; set; }

    public string ClassCode { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; }

    public Guid? TeacherId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? InviteCode { get; set; }

    public DateTime? InviteCodeExpiresAt { get; set; }

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    public virtual Semester Semester { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual User? Teacher { get; set; }
}
