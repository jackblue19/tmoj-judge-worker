using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserSession
{
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceId { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual User User { get; set; } = null!;
}
