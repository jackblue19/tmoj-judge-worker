using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ProblemEditorial
{
    public Guid Id { get; set; }

    public Guid ProblemId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Problem Problem { get; set; } = null!;
}
