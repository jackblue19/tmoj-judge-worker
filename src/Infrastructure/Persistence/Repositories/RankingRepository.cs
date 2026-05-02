using Application.Common.Interfaces;
using Application.UseCases.Ranking.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RankingRepository : IRankingRepository
{
    private readonly TmojDbContext _db;

    public RankingRepository(TmojDbContext db) => _db = db;

    public async Task<GlobalLeaderboardDto> GetGlobalLeaderboardAsync(
        int page, int pageSize, string? search,
        Guid? subjectId, Guid? semesterId,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var statsQuery = _db.UserProblemStats
            .AsNoTracking()
            .Where(s => s.Solved && s.Problem.IsActive);

        if (subjectId.HasValue || semesterId.HasValue)
        {
            var filteredProblemIds = _db.ClassSlotProblems
                .AsNoTracking()
                .Where(csp =>
                    (!subjectId.HasValue || csp.Slot.ClassSemester.SubjectId == subjectId.Value) &&
                    (!semesterId.HasValue || csp.Slot.ClassSemester.SemesterId == semesterId.Value))
                .Select(csp => csp.ProblemId)
                .Distinct();

            statsQuery = statsQuery.Where(s => filteredProblemIds.Contains(s.ProblemId));
        }

        var grouped = statsQuery
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Solved = g.Count(),
                TotalAttempts = g.Sum(x => x.Attempts),
            });

        var joined = grouped.Join(
            _db.Users,
            g => g.UserId,
            u => u.UserId,
            (g, u) => new
            {
                g.UserId,
                g.Solved,
                g.TotalAttempts,
                u.Username,
                u.DisplayName,
                u.FirstName,
                u.LastName,
                u.AvatarUrl,
                u.RollNumber,
                u.MemberCode,
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            joined = joined.Where(x =>
                x.Username.ToLower().Contains(s) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)) ||
                (x.FirstName + " " + x.LastName).ToLower().Contains(s) ||
                (x.RollNumber != null && x.RollNumber.ToLower().Contains(s)) ||
                (x.MemberCode != null && x.MemberCode.ToLower().Contains(s)));
        }

        var ordered = joined
            .OrderByDescending(x => x.Solved)
            .ThenByDescending(x => x.TotalAttempts == 0
                ? 0
                : x.Solved * 100 / x.TotalAttempts);

        var total = await ordered.CountAsync(ct);

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = rows.Select(x => x.UserId).ToList();
        var equippedFramesList = await _db.UserInventories
            .AsNoTracking()
            .Where(ui => userIds.Contains(ui.UserId) && ui.IsEquipped && ui.Item.ItemType == "avatar_frame")
            .Select(ui => new { ui.UserId, ui.Item.ImageUrl })
            .ToListAsync(ct);

        var equippedFrames = equippedFramesList
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().ImageUrl);

        var ranked = rows.Select((x, i) => new GlobalLeaderboardRowDto
        {
            Rank = (page - 1) * pageSize + i + 1,
            UserId = x.UserId,
            Username = x.Username,
            Fullname = x.DisplayName ?? $"{x.FirstName} {x.LastName}".Trim(),
            AvatarUrl = x.AvatarUrl,
            EquippedFrameUrl = equippedFrames.TryGetValue(x.UserId, out var frame) ? frame : null,
            RollNumber = x.RollNumber ?? x.MemberCode,
            Solved = x.Solved,
            TotalAttempts = x.TotalAttempts,
            Accuracy = x.TotalAttempts == 0
                ? 0
                : (int)Math.Round(x.Solved * 100.0 / x.TotalAttempts),
            Points = x.Solved * 10,
        }).ToList();

        return new GlobalLeaderboardDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Rows = ranked,
        };
    }

    public async Task<List<PublicContestSummaryDto>> GetPublicContestsAsync(
        Guid? subjectId, Guid? semesterId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        IQueryable<Contest> contestQuery;

        if (subjectId.HasValue || semesterId.HasValue)
        {
            var classContestIds = _db.ClassSlots
                .AsNoTracking()
                .Where(cs => cs.ContestId != null
                    && (!subjectId.HasValue || cs.ClassSemester.SubjectId == subjectId.Value)
                    && (!semesterId.HasValue || cs.ClassSemester.SemesterId == semesterId.Value))
                .Select(cs => cs.ContestId!.Value)
                .Distinct();

            contestQuery = _db.Contests
                .AsNoTracking()
                .Where(c => c.IsActive
                    && c.Status != "draft" && c.Status != "cancelled"
                    && classContestIds.Contains(c.Id));
        }
        else
        {
            contestQuery = _db.Contests
                .AsNoTracking()
                .Where(c => c.VisibilityCode == "public"
                         && c.IsActive
                         && c.Status != "draft" && c.Status != "cancelled");
        }

        return await contestQuery
            .OrderByDescending(c => c.StartAt)
            .Select(c => new PublicContestSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                Rules = c.Rules,
                Status = now < c.StartAt ? "upcoming"
                       : now > c.EndAt   ? "ended"
                       : "running",
            })
            .ToListAsync(ct);
    }

    public async Task<bool> IsPublicContestAsync(Guid contestId, CancellationToken ct = default) =>
        await _db.Contests
            .AsNoTracking()
            .AnyAsync(c => c.Id == contestId
                        && c.VisibilityCode == "public"
                        && c.IsActive
                        && c.Status != "draft" && c.Status != "cancelled", ct);

    public async Task<bool> ContestExistsAsync(Guid contestId, CancellationToken ct = default) =>
        await _db.Contests
            .AsNoTracking()
            .AnyAsync(c => c.Id == contestId
                        && c.IsActive
                        && c.Status != "draft" && c.Status != "cancelled", ct);
}
