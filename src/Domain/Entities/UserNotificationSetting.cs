using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserNotificationSetting
{
    public Guid SettingId { get; set; }

    public Guid UserId { get; set; }

    public bool ReceiveEmail { get; set; }

    public bool ReceivePush { get; set; }

    public bool ReceiveSystem { get; set; }

    public virtual User User { get; set; } = null!;
}
