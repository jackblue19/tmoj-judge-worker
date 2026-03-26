using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Editorial
{
    public Guid EditorialId { get; set; }

    public Guid ProblemId { get; set; }

    public Guid? AuthorId { get; set; }

    public Guid StorageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Author { get; set; }

    public virtual StorageFile Storage { get; set; } = null!;
}
