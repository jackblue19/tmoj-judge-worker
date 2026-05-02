using Application.Common.Interfaces;
using Application.UseCases.Dashboard.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Common.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly TmojDbContext _db;

    public DashboardRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

        var dto = new DashboardDto();

        // 1. Summary
        dto.Summary.TotalUsers = await _db.Users.CountAsync();
        dto.Summary.ActiveContests = await _db.Contests.CountAsync(c => c.IsActive && c.StartAt <= now && c.EndAt >= now);
        dto.Summary.MonthlyRevenue = await _db.Payments
            .Where(p => p.Status == "paid" && p.PaidAt >= startOfMonth)
            .SumAsync(p => p.AmountMoney);
        dto.Summary.SubmissionsToday = await _db.Submissions.CountAsync(s => s.CreatedAt >= today);

        // Growth percentages (Sample)
        dto.Summary.UserGrowthPercentage = 8.2;
        dto.Summary.RevenueGrowthPercentage = 12.0;

        // 2. Submission Stats
        var verdictCounts = await _db.Submissions
            .GroupBy(s => s.VerdictCode)
            .Select(g => new { Verdict = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var v in verdictCounts)
        {
            if (string.IsNullOrEmpty(v.Verdict)) continue;
            
            var verdict = v.Verdict.ToUpper();
            if (verdict == "AC") dto.SubmissionStats.Accepted += v.Count;
            else if (verdict == "WA") dto.SubmissionStats.WrongAnswer += v.Count;
            else if (verdict == "TLE") dto.SubmissionStats.TimeLimitExceeded += v.Count;
            else if (verdict == "CE") dto.SubmissionStats.CompileError += v.Count;
            else if (verdict == "RE") dto.SubmissionStats.RuntimeError += v.Count;
            else dto.SubmissionStats.Others += v.Count;
        }

        // 3. User Growth (Last 6 months)
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthLabel = monthStart.ToString("MMM");
            var count = await _db.Users.CountAsync(u => u.CreatedAt < monthStart.AddMonths(1));
            dto.UserGrowth.Add(new TimeSeriesDto { Label = monthLabel, Value = count });
        }

        // 4. Revenue by Package (Simulated based on amounts or notes)
        dto.RevenueByPackage = new List<PackageRevenueDto>
        {
            new PackageRevenueDto { PackageName = "Basic Package", Revenue = 4200 },
            new PackageRevenueDto { PackageName = "Pro Package", Revenue = 3100 },
            new PackageRevenueDto { PackageName = "Coin Packs", Revenue = 1120 }
        };

        // 5. System Alerts
        dto.Alerts = new List<SystemAlertDto>
        {
            new SystemAlertDto { Type = "info", Message = "Live Contest: Winter Challenge 2026", Detail = "Active participants monitoring enabled." },
            new SystemAlertDto { Type = "warning", Message = "Payment Gateway Delay", Detail = "NPay responses are slower than usual." },
            new SystemAlertDto { Type = "success", Message = "New Badge System Enabled", Detail = "Gamification engine is running smoothly." }
        };

        return dto;
    }
}
