using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Collection
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public bool IsVisibility { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    public virtual User User { get; set; } = null!;
}
