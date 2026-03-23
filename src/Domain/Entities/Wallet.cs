using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("wallet")]
[Index("UserId", "Currency", Name = "unique_user_currency_wallet", IsUnique = true)]
public partial class Wallet
{
    [Key]
    [Column("wallet_id")]
    public Guid WalletId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("balance")]
    [Precision(18, 2)]
    public decimal Balance { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Wallets")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Wallet")]
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
