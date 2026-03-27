using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserPermission
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTime EffectiveAt { get; set; }

    public DateTime? ExpireAt { get; set; }

    public bool Status { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
