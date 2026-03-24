using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("content_reports")]
public partial class ContentReport
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("reporter_id")]
    public Guid ReporterId { get; set; }

    [Column("target_type")]
    [StringLength(50)]
    public string TargetType { get; set; } = null!;

    [Column("target_id")]
    public Guid TargetId { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = null!;

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Report")]
    public virtual ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();

    [ForeignKey("ReporterId")]
    [InverseProperty("ContentReports")]
    public virtual User Reporter { get; set; } = null!;
}
