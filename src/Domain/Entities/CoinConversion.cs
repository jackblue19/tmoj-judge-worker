using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CoinConversion
{
    public Guid ConversionId { get; set; }

    public Guid PaymentId { get; set; }

    public Guid TransactionId { get; set; }

    public decimal Rate { get; set; }

    public decimal CoinAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;

    public virtual WalletTransaction Transaction { get; set; } = null!;
}
