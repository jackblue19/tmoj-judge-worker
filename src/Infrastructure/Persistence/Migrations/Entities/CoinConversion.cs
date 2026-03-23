using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("coin_conversion")]
public partial class CoinConversion
{
    [Key]
    [Column("conversion_id")]
    public Guid ConversionId { get; set; }

    [Column("payment_id")]
    public Guid PaymentId { get; set; }

    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("rate")]
    [Precision(18, 6)]
    public decimal Rate { get; set; }

    [Column("coin_amount")]
    [Precision(18, 2)]
    public decimal CoinAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("CoinConversions")]
    public virtual Payment Payment { get; set; } = null!;

    [ForeignKey("TransactionId")]
    [InverseProperty("CoinConversions")]
    public virtual WalletTransaction Transaction { get; set; } = null!;
}
