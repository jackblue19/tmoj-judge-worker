using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("wallet_transaction")]
public partial class WalletTransaction
{
    [Key]
    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("wallet_id")]
    public Guid WalletId { get; set; }

    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("direction")]
    public string Direction { get; set; } = null!;

    [Column("amount")]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Column("source_type")]
    public string SourceType { get; set; } = null!;

    [Column("source_id")]
    public Guid? SourceId { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Transaction")]
    public virtual ICollection<CoinConversion> CoinConversions { get; set; } = new List<CoinConversion>();

    [ForeignKey("WalletId")]
    [InverseProperty("WalletTransactions")]
    public virtual Wallet Wallet { get; set; } = null!;
}
