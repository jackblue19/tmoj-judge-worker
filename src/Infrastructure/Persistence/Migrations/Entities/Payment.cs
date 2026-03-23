using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("payment")]
public partial class Payment
{
    [Key]
    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("provider_name")]
    public string? ProviderName { get; set; }

    [Column("provider_tx_id")]
    public string? ProviderTxId { get; set; }

    [Column("bank_code")]
    public string? BankCode { get; set; }

    [Column("payment_txn")]
    public string? PaymentTxn { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = null!;

    [Column("amount_money")]
    [Precision(18, 2)]
    public decimal AmountMoney { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = null!;

    [Column("note")]
    public string? Note { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [InverseProperty("Payment")]
    public virtual ICollection<CoinConversion> CoinConversions { get; set; } = new List<CoinConversion>();

    [ForeignKey("UserId")]
    [InverseProperty("Payments")]
    public virtual User? User { get; set; }
}
