using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("provider")]
[Index("ProviderCode", Name = "provider_provider_code_key", IsUnique = true)]
public partial class Provider
{
    [Key]
    [Column("provider_id")]
    public Guid ProviderId { get; set; }

    [Column("provider_code")]
    public string ProviderCode { get; set; } = null!;

    [Column("provider_display_name")]
    public string ProviderDisplayName { get; set; } = null!;

    [Column("provider_icon")]
    public string? ProviderIcon { get; set; }

    [Column("issuer")]
    public string? Issuer { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [InverseProperty("Provider")]
    public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
