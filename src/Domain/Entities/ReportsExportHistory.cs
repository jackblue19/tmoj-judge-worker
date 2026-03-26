using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ReportsExportHistory
{
    public Guid ExportId { get; set; }

    public Guid? GeneratedBy { get; set; }

    public string ReportType { get; set; } = null!;

    public string? FilePath { get; set; }

    public string Status { get; set; } = null!;

    public string ExtensionType { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public virtual User? GeneratedByNavigation { get; set; }
}
