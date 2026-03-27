using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class WalletTransaction
{
    public Guid TransactionId { get; set; }

    public Guid WalletId { get; set; }

    public string Type { get; set; } = null!;

    public string Direction { get; set; } = null!;

    public decimal Amount { get; set; }

    public string SourceType { get; set; } = null!;

    public Guid? SourceId { get; set; }

    public string Status { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CoinConversion> CoinConversions { get; set; } = new List<CoinConversion>();

    public virtual Wallet Wallet { get; set; } = null!;
}
