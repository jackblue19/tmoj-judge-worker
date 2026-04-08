using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Reports.Dtos;

public class ReportGroupDto
{
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = null!;

    public int TotalReports { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }

    public DateTime? LatestCreatedAt { get; set; }

    public List<string> Reasons { get; set; } = new();
}