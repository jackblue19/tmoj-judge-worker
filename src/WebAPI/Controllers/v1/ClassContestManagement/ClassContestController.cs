using Asp.Versioning;
using Domain.Entities;
using Class = Domain.Entities.Class;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ClassContestManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class/{classId:guid}/contests")]
[Authorize]
public class ClassContestController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ClassContestController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // GET .../contests  →  List contests in a class
    // ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid classId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var contestSlots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.Contest).ThenInclude(c => c!.ContestProblems)
                .Include(s => s.Contest).ThenInclude(c => c!.ContestTeams)
                .Where(s => s.ClassId == classId && s.Mode == "contest" && s.ContestId != null)
                .ToListAsync(ct);

            var result = contestSlots
                .Where(s => s.Contest != null)
                .Select(s => new ClassContestSummaryResponse(
                    s.Contest!.Id,
                    s.Contest.Title,
                    s.Contest.Slug,
                    s.Contest.StartAt,
                    s.Contest.EndAt,
                    s.Contest.IsActive,
                    s.Contest.ContestProblems.Count,
                    s.Contest.ContestTeams.Count))
                .ToList();

            return Ok(ApiResponse<List<ClassContestSummaryResponse>>.Ok(result, "Contests fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching contests." });
        }
    }

    // ──────────────────────────────────────────
    // POST .../contests  →  Create Class's Contest (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid classId,
        [FromBody] CreateClassContestRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != userId) return Forbid();

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { Message = "Title is required." });
            if (req.StartAt >= req.EndAt)
                return BadRequest(new { Message = "EndAt must be after StartAt." });

            // 1. Create Contest entity
            var contest = new Contest
            {
                Title = req.Title.Trim(),
                Slug = req.Slug?.Trim(),
                DescriptionMd = req.DescriptionMd?.Trim(),
                VisibilityCode = "private",
                ContestType = "class",
                AllowTeams = false,
                StartAt = req.StartAt,
                EndAt = req.EndAt,
                FreezeAt = req.FreezeAt,
                Rules = req.Rules?.Trim(),
                IsActive = true,
                CreatedBy = userId
            };
            _db.Contests.Add(contest);

            // 2. Add contest problems
            if (req.Problems is { Count: > 0 })
            {
                int ord = 1;
                foreach (var p in req.Problems)
                {
                    if (!await _db.Problems.AnyAsync(pr => pr.Id == p.ProblemId, ct))
                        return BadRequest(new { Message = $"Problem {p.ProblemId} not found." });

                    _db.ContestProblems.Add(new ContestProblem
                    {
                        ContestId = contest.Id,
                        ProblemId = p.ProblemId,
                        Ordinal = p.Ordinal ?? ord,
                        Alias = p.Alias,
                        Points = p.Points,
                        MaxScore = p.MaxScore,
                        TimeLimitMs = p.TimeLimitMs,
                        MemoryLimitKb = p.MemoryLimitKb,
                        IsActive = true,
                        CreatedBy = userId
                    });
                    ord++;
                }
            }

            // 3. Create ClassSlot linked to this contest
            int slotNo = req.SlotNo ?? (await _db.ClassSlots
                .Where(s => s.ClassId == classId)
                .MaxAsync(s => (int?)s.SlotNo, ct) ?? 0) + 1;

            var slot = new ClassSlot
            {
                ClassId = classId,
                SlotNo = slotNo,
                Title = req.SlotTitle ?? req.Title.Trim(),
                Mode = "contest",
                ContestId = contest.Id,
                IsPublished = false,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            _db.ClassSlots.Add(slot);

            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { classId, contestId = contest.Id },
                new { Message = "Contest created successfully.", contest.Id, SlotId = slot.Id });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the contest." });
        }
    }

    // ──────────────────────────────────────────
    // GET .../contests/{contestId}  →  View Contest (Authenticated)
    // ──────────────────────────────────────────
    [HttpGet("{contestId:guid}")]
    public async Task<IActionResult> GetById(
        Guid classId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            // Verify contest belongs to this class
            var slot = await _db.ClassSlots.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ClassId == classId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class." });

            var contest = await _db.Contests.AsNoTracking()
                .Include(c => c.ContestProblems).ThenInclude(cp => cp.Problem)
                .Include(c => c.ContestTeams)
                .FirstOrDefaultAsync(c => c.Id == contestId, ct);
            if (contest is null) return NotFound(new { Message = "Contest not found." });

            // Check if user has joined
            var isJoined = contest.ContestTeams
                .Any(ct2 => ct2.Team != null && ct2.Team.LeaderId == userId);

            // Also check via team members if needed
            if (!isJoined)
            {
                var userTeamIds = await _db.TeamMembers.AsNoTracking()
                    .Where(tm => tm.UserId == userId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync(ct);
                isJoined = contest.ContestTeams.Any(ct2 => userTeamIds.Contains(ct2.TeamId));
            }

            var now = DateTime.UtcNow;
            double? remaining = contest.EndAt > now ? (contest.EndAt - now).TotalSeconds : 0;

            var dto = new ClassContestResponse(
                contest.Id, classId, slot.Id,
                contest.Title, contest.Slug, contest.DescriptionMd, contest.Rules,
                contest.StartAt, contest.EndAt, contest.FreezeAt,
                contest.IsActive, isJoined, remaining,
                contest.CreatedAt,
                contest.ContestProblems.OrderBy(cp => cp.Ordinal).Select(cp => new ContestProblemResponse(
                    cp.Id, cp.ProblemId, cp.Problem.Title, cp.Problem.Slug,
                    cp.Alias, cp.Ordinal, cp.Points, cp.MaxScore,
                    cp.TimeLimitMs, cp.MemoryLimitKb)).ToList());

            return Ok(ApiResponse<ClassContestResponse>.Ok(dto, "Contest fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the contest." });
        }
    }

    // ──────────────────────────────────────────
    // PUT .../contests/{contestId}/extend  →  Extend Contest's Time (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{contestId:guid}/extend")]
    public async Task<IActionResult> ExtendTime(
        Guid classId, Guid contestId,
        [FromBody] ExtendContestRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != userId) return Forbid();

            // Verify contest belongs to class
            var slot = await _db.ClassSlots.FirstOrDefaultAsync(
                s => s.ClassId == classId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class." });

            var contest = await _db.Contests.FirstOrDefaultAsync(c => c.Id == contestId, ct);
            if (contest is null) return NotFound(new { Message = "Contest not found." });

            if (req.NewEndAt <= contest.EndAt)
                return BadRequest(new { Message = "New end time must be after current end time." });

            contest.EndAt = req.NewEndAt;
            contest.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Contest time extended successfully.", contest.EndAt });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while extending contest time." });
        }
    }

    // ──────────────────────────────────────────
    // POST .../contests/{contestId}/join  →  Join Contest (Student)
    // ──────────────────────────────────────────
    [HttpPost("{contestId:guid}/join")]
    public async Task<IActionResult> Join(
        Guid classId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            // Verify class membership
            var isMember = await _db.ClassMembers.AsNoTracking()
                .AnyAsync(m => m.ClassSemester.ClassId == classId && m.UserId == userId.Value && m.IsActive, ct);
            if (!isMember) return Forbid();

            // Verify contest belongs to class
            var slot = await _db.ClassSlots.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ClassId == classId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class." });

            var contest = await _db.Contests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contestId && c.IsActive, ct);
            if (contest is null) return NotFound(new { Message = "Contest not found or inactive." });

            var now = DateTime.UtcNow;
            if (now < contest.StartAt)
                return BadRequest(new { Message = "Contest has not started yet." });
            if (now >= contest.EndAt)
                return BadRequest(new { Message = "Contest has already ended." });

            // Find or create personal team
            var personalTeam = await _db.Teams
                .FirstOrDefaultAsync(t => t.LeaderId == userId.Value && t.IsPersonal, ct);

            if (personalTeam is null)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value, ct);
                personalTeam = new Team
                {
                    LeaderId = userId.Value,
                    TeamSize = 1,
                    TeamName = user?.DisplayName ?? "Personal Team",
                    IsPersonal = true
                };
                _db.Teams.Add(personalTeam);
                _db.TeamMembers.Add(new TeamMember
                {
                    TeamId = personalTeam.Id,
                    UserId = userId.Value
                });
            }

            // Check if already joined
            var alreadyJoined = await _db.ContestTeams
                .AnyAsync(ct2 => ct2.ContestId == contestId && ct2.TeamId == personalTeam.Id, ct);
            if (alreadyJoined) return Conflict(new { Message = "You have already joined this contest." });

            _db.ContestTeams.Add(new ContestTeam
            {
                ContestId = contestId,
                TeamId = personalTeam.Id
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Joined contest successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while joining the contest." });
        }
    }

    // ── Helpers ───────────────────────────────

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : null;
    }
}
