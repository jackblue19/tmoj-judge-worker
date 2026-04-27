using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class FptItem
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string ItemType { get; set; } = null!;

    public decimal PriceCoin { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetaJson { get; set; } // Map với jsonb

    public int? DurationDays { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual ICollection<UserInventory> UserInventories { get; set; } = new List<UserInventory>();
}
