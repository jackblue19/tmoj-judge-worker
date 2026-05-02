using System;
using System.Collections.Generic;

namespace Application.UseCases.Dashboard.Dtos;

public class DashboardDto
{
    public SummaryDto Summary { get; set; } = new();
    public EngagementDto Engagement { get; set; } = new();
    public ProblemDistributionDto ProblemStats { get; set; } = new();
    public EconomyDto Economy { get; set; } = new();
    public OperationalDto Operational { get; set; } = new();
    public List<TimeSeriesDto> UserGrowth { get; set; } = new();
    public List<PackageRevenueDto> RevenueByPackage { get; set; } = new();
    public SubmissionVerdictStatsDto SubmissionStats { get; set; } = new();
    public List<SystemAlertDto> Alerts { get; set; } = new();
}

public class EngagementDto
{
    public int DAU { get; set; }
    public int MAU { get; set; }
    public int OnlineNow { get; set; }
}

public class ProblemDistributionDto
{
    public int Easy { get; set; }
    public int Medium { get; set; }
    public int Hard { get; set; }
}

public class EconomyDto
{
    public decimal TotalCoinsInWorld { get; set; }
    public int ItemsSoldToday { get; set; }
}

public class OperationalDto
{
    public int PendingSubmissions { get; set; }
    public double AverageJudgeLatencySeconds { get; set; }
}

public class SummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveContests { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int SubmissionsToday { get; set; }
    public double UserGrowthPercentage { get; set; }
    public double RevenueGrowthPercentage { get; set; }
}

public class TimeSeriesDto
{
    public string Label { get; set; } = string.Empty; // e.g. "Aug", "Sep"
    public int Value { get; set; }
}

public class PackageRevenueDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class SubmissionVerdictStatsDto
{
    public int Accepted { get; set; }
    public int WrongAnswer { get; set; }
    public int TimeLimitExceeded { get; set; }
    public int CompileError { get; set; }
    public int RuntimeError { get; set; }
    public int Others { get; set; }
}

public class SystemAlertDto
{
    public string Type { get; set; } = string.Empty; // "info" | "warning" | "error" | "success"
    public string Message { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
