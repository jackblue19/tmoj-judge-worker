using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("moderation_actions")]
public partial class ModerationAction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("report_id")]
    public Guid ReportId { get; set; }

    [Column("admin_id")]
    public Guid AdminId { get; set; }

    [Column("action_type")]
    [StringLength(50)]
    public string ActionType { get; set; } = null!;

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("ModerationActions")]
    public virtual User Admin { get; set; } = null!;

    [ForeignKey("ReportId")]
    [InverseProperty("ModerationActions")]
    public virtual ContentReport Report { get; set; } = null!;
}
