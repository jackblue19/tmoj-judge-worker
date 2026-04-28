using System;

namespace Domain.Entities;

public partial class UserInventory
{
    public Guid InventoryId { get; set; }

    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    public DateTime AcquiredAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsEquipped { get; set; }

    public Guid? TransactionId { get; set; }
    public int Quantity { get; set; } = 1;

    public virtual FptItem Item { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual WalletTransaction? Transaction { get; set; }
}
