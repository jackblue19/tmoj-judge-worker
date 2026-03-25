using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class EmailVerification
{
    public Guid VerificationId { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
