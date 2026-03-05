using System;
using System.Collections.Generic;

Domain.Entities

public partial class CollectionItem
{
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }

    public Guid? ProblemId { get; set; }

    public Guid? ContestId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Collection Collection { get; set; } = null!;

    public virtual Contest? Contest { get; set; }

    public virtual Problem? Problem { get; set; }
}
