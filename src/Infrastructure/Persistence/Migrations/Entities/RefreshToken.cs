using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("refresh_token")]
[Index("TokenHash", Name = "refresh_token_token_hash_key", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    [Column("token_id")]
    public Guid TokenId { get; set; }

    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("token_hash")]
    public string TokenHash { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expire_at")]
    public DateTime ExpireAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("replaced_by_token_id")]
    public Guid? ReplacedByTokenId { get; set; }

    [InverseProperty("ReplacedByToken")]
    public virtual ICollection<RefreshToken> InverseReplacedByToken { get; set; } = new List<RefreshToken>();

    [ForeignKey("ReplacedByTokenId")]
    [InverseProperty("InverseReplacedByToken")]
    public virtual RefreshToken? ReplacedByToken { get; set; }

    [ForeignKey("SessionId")]
    [InverseProperty("RefreshTokens")]
    public virtual UserSession Session { get; set; } = null!;
}
