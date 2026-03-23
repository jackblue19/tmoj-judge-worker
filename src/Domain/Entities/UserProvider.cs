using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("user_provider")]
[Index("UserId", "ProviderId", Name = "unique_user_provider", IsUnique = true)]
public partial class UserProvider
{
    [Key]
    [Column("user_provider_id")]
    public Guid UserProviderId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("provider_id")]
    public Guid ProviderId { get; set; }

    [Column("provider_subject")]
    public string ProviderSubject { get; set; } = null!;

    [Column("provider_email")]
    public string? ProviderEmail { get; set; }

    [Column("provider_profile", TypeName = "jsonb")]
    public string? ProviderProfile { get; set; }

    [Column("access_token_enc")]
    public string? AccessTokenEnc { get; set; }

    [Column("refresh_token_enc")]
    public string? RefreshTokenEnc { get; set; }

    [Column("token_type")]
    public string? TokenType { get; set; }

    [Column("scope")]
    public string? Scope { get; set; }

    [Column("expire_at")]
    public DateTime? ExpireAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ProviderId")]
    [InverseProperty("UserProviders")]
    public virtual Provider Provider { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserProviders")]
    public virtual User User { get; set; } = null!;
}
