using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid? UserId { get; set; }

    public string? ProviderName { get; set; }

    public string? ProviderTxId { get; set; }

    public string? BankCode { get; set; }

    public string? PaymentTxn { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal AmountMoney { get; set; }

    public string Currency { get; set; } = null!;

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual ICollection<CoinConversion> CoinConversions { get; set; } = new List<CoinConversion>();

    public virtual User? User { get; set; }
}
