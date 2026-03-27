using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserProvider
{
    public Guid UserProviderId { get; set; }

    public Guid UserId { get; set; }

    public Guid ProviderId { get; set; }

    public string ProviderSubject { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    public string? ProviderProfile { get; set; }

    public string? AccessTokenEnc { get; set; }

    public string? RefreshTokenEnc { get; set; }

    public string? TokenType { get; set; }

    public string? Scope { get; set; }

    public DateTime? ExpireAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Provider Provider { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
