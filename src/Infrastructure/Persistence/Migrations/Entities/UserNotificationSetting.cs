using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("user_notification_settings")]
[Index("UserId", Name = "user_notification_settings_user_id_key", IsUnique = true)]
public partial class UserNotificationSetting
{
    [Key]
    [Column("setting_id")]
    public Guid SettingId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("receive_email")]
    public bool ReceiveEmail { get; set; }

    [Column("receive_push")]
    public bool ReceivePush { get; set; }

    [Column("receive_system")]
    public bool ReceiveSystem { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserNotificationSetting")]
    public virtual User User { get; set; } = null!;
}
