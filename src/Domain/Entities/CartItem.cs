using System;

namespace Domain.Entities;

public partial class CartItem
{
    public Guid CartItemId { get; set; }

    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual FptItem Item { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
