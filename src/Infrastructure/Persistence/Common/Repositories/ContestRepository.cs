using Application.Common.Interfaces;
using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class ContestRepository : IContestRepository
{
    private readonly TmojDbContext _db;

    public ContestRepository(TmojDbContext db)
    {
        _db = db;
    }

    // =============================================
    // GET CONTEST LIST
    // =============================================
    public async Task<PagedResult<ContestDto>> GetContestsAsync(
        string? status,
        int page,
        int pageSize)
    {
        var now = DateTime.UtcNow;

        var query = _db.Contests
            .AsNoTracking()
            .Where(x => x.VisibilityCode == "public");

        if (!string.IsNullOrEmpty(status))
        {
            status = status.ToLower();

            if (status == "upcoming")
                query = query.Where(x => x.StartAt > now);
            else if (status == "running")
                query = query.Where(x => x.StartAt <= now && x.EndAt >= now);
            else if (status == "ended")
                query = query.Where(x => x.EndAt < now);
        }

        var all = await query.ToListAsync();

        var sorted = all
            .Select(x => new
            {
                Contest = x,
                Priority =
                    x.StartAt <= now && x.EndAt >= now ? 0 :
                    x.StartAt > now ? 1 :
                    2
            })
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Contest.StartAt)
            .Select(x => x.Contest)
            .ToList();

        var total = sorted.Count;

        var paged = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = paged.Select(x => new ContestDto
        {
            Id = x.Id,
            Title = x.Title,
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            VisibilityCode = x.VisibilityCode,
            ContestType = x.ContestType,
            AllowTeams = x.AllowTeams,
            Status =
                x.StartAt > now ? "upcoming" :
                x.EndAt < now ? "ended" :
                "running"
        }).ToList();

        return new PagedResult<ContestDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // =============================================
    // GET DETAIL (FIX CHUẨN DTO MỚI)
    // =============================================
    public async Task<ContestDetailDto?> GetContestDetailAsync(Guid contestId)
    {
        var now = DateTime.UtcNow;

        var contest = await _db.Contests
            .AsNoTracking()
            .Include(c => c.ContestProblems!)
                .ThenInclude(cp => cp.Problem)
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null) return null;

        var problems = contest.ContestProblems!
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayIndex ?? p.Ordinal ?? 999)
            .ThenBy(p => p.Alias)
            .ToList();

        return new ContestDetailDto
        {
            Id = contest.Id,
            Title = contest.Title,
            Description = contest.DescriptionMd ?? "",
            Slug = contest.Slug ?? "",

            // ✅ FIX CHÍNH
            Visibility = contest.VisibilityCode,

            ContestType = contest.ContestType ?? "icpc",
            AllowTeams = contest.AllowTeams,

            Status =
                contest.StartAt > now ? "upcoming" :
                contest.EndAt < now ? "ended" :
                "running",

            IsPublished = contest.VisibilityCode == "public",

            StartAt = contest.StartAt,
            EndAt = contest.EndAt,
            DurationMinutes = (int)(contest.EndAt - contest.StartAt).TotalMinutes,

            ProblemCount = problems.Count,
            TotalPoints = problems.Sum(p => p.Points ?? 0),

            Problems = problems.Select(p => new ContestProblemDto
            {
                Id = p.Id,
                ProblemId = p.ProblemId,
                Title = p.Problem.Title,
                Alias = p.Alias,
                Ordinal = p.Ordinal,
                DisplayIndex = p.DisplayIndex,
                Points = p.Points ?? 0,
                TimeLimitMs = p.TimeLimitMs,
                MemoryLimitKb = p.MemoryLimitKb
            }).ToList()
        };
    }

    // =============================================
    // CHECK JOIN
    // =============================================
    public async Task<bool> IsTeamJoinedAsync(Guid contestId, Guid teamId)
    {
        return await _db.ContestTeams
            .AnyAsync(x => x.ContestId == contestId && x.TeamId == teamId);
    }

    public async Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId)
    {
        return await _db.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.UserId == userId);
    }
}