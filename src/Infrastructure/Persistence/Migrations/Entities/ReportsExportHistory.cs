using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("reports_export_history")]
public partial class ReportsExportHistory
{
    [Key]
    [Column("export_id")]
    public Guid ExportId { get; set; }

    [Column("generated_by")]
    public Guid? GeneratedBy { get; set; }

    [Column("report_type")]
    public string ReportType { get; set; } = null!;

    [Column("file_path")]
    public string? FilePath { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("extension_type")]
    public string ExtensionType { get; set; } = null!;

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [ForeignKey("GeneratedBy")]
    [InverseProperty("ReportsExportHistories")]
    public virtual User? GeneratedByNavigation { get; set; }
}
