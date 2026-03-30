using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Class
{
    public Guid ClassId { get; set; }

    public string ClassCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ClassSemester> ClassSemesters { get; set; } = new List<ClassSemester>();
}
