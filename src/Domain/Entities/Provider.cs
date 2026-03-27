using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Provider
{
    public Guid ProviderId { get; set; }

    public string ProviderCode { get; set; } = null!;

    public string ProviderDisplayName { get; set; } = null!;

    public string? ProviderIcon { get; set; }

    public string? Issuer { get; set; }

    public bool Enabled { get; set; }

    public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
