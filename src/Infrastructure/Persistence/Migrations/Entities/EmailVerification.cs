using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("email_verification")]
[Index("UserId", Name = "unique_verification_user", IsUnique = true)]
public partial class EmailVerification
{
    [Key]
    [Column("verification_id")]
    public Guid VerificationId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("token")]
    public string Token { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmailVerification")]
    public virtual User User { get; set; } = null!;
}
