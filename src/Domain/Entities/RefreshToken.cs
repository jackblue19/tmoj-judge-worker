using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class RefreshToken
{
    public Guid TokenId { get; set; }

    public Guid SessionId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpireAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public virtual ICollection<RefreshToken> InverseReplacedByToken { get; set; } = new List<RefreshToken>();

    public virtual RefreshToken? ReplacedByToken { get; set; }

    public virtual UserSession Session { get; set; } = null!;
}
