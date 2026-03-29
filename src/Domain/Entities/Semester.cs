using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Semester
{
    public Guid SemesterId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly StartAt { get; set; }

    public DateOnly EndAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClassSemester> ClassSemesters { get; set; } = new List<ClassSemester>();
}
