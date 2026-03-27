using System;

namespace Domain.Entities;

public partial class ClassSemester
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid SemesterId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();
}
