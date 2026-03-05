using Asp.Versioning;
using Domain.Entities;
using Class = Domain.Entities.Class;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ClassSlotManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class/{classId:guid}/slots")]
[Authorize]
public class ClassSlotController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ClassSlotController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // GET .../slots  →  List all slots for a class
    // ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid classId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var slots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems).ThenInclude(sp => sp.Problem)
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.SlotNo)
                .ToListAsync(ct);

            var result = slots.Select(s => new ClassSlotResponse(
                s.Id, s.ClassId, s.SlotNo, s.Title, s.Description, s.Rules,
                s.OpenAt, s.DueAt, s.CloseAt, s.Mode, s.ContestId, s.IsPublished,
                s.CreatedAt, s.UpdatedAt,
                s.ClassSlotProblems.OrderBy(sp => sp.Ordinal).Select(sp => new SlotProblemResponse(
                    sp.ProblemId, sp.Problem.Title, sp.Problem.Slug,
                    sp.Ordinal, sp.Points, sp.IsRequired)).ToList()
            )).ToList();

            return Ok(ApiResponse<List<ClassSlotResponse>>.Ok(result, "Slots fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching slots." });
        }
    }

    // ──────────────────────────────────────────
    // POST .../slots  →  Create Assignment (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid classId,
        [FromBody] CreateClassSlotRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != userId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { Message = "Title is required." });

            var mode = req.Mode?.ToLowerInvariant() ?? "problemset";
            if (mode is not ("problemset" or "contest"))
                return BadRequest(new { Message = "Mode must be 'problemset' or 'contest'." });

            // Check unique slot_no
            if (await _db.ClassSlots.AnyAsync(s => s.ClassId == classId && s.SlotNo == req.SlotNo, ct))
                return Conflict(new { Message = $"SlotNo {req.SlotNo} already exists in this class." });

            var slot = new ClassSlot
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                SlotNo = req.SlotNo,
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                Rules = req.Rules?.Trim(),
                OpenAt = req.OpenAt,
                DueAt = req.DueAt,
                CloseAt = req.CloseAt,
                Mode = mode,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };

            _db.ClassSlots.Add(slot);

            // Add problems
            if (req.Problems is { Count: > 0 })
            {
                foreach (var p in req.Problems)
                {
                    if (!await _db.Problems.AnyAsync(pr => pr.Id == p.ProblemId, ct))
                        return BadRequest(new { Message = $"Problem {p.ProblemId} not found." });

                    _db.ClassSlotProblems.Add(new ClassSlotProblem
                    {
                        SlotId = slot.Id,
                        ProblemId = p.ProblemId,
                        Ordinal = p.Ordinal,
                        Points = p.Points,
                        IsRequired = p.IsRequired
                    });
                }
            }

            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetAll), new { classId },
                new { Message = "Assignment created successfully.", slot.Id, slot.SlotNo });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the assignment." });
        }
    }

    // ──────────────────────────────────────────
    // PUT .../slots/{slotId}/due-date  →  Set Due Date (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{slotId:guid}/due-date")]
    public async Task<IActionResult> SetDueDate(
        Guid classId, Guid slotId,
        [FromBody] SetDueDateRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != userId)
                return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassId == classId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            slot.DueAt = req.DueAt;
            if (req.CloseAt.HasValue) slot.CloseAt = req.CloseAt.Value;
            slot.UpdatedAt = DateTime.UtcNow;
            slot.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Due date updated successfully.", slot.DueAt, slot.CloseAt });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while setting due date." });
        }
    }

    // ──────────────────────────────────────────
    // PUT .../slots/{slotId}/publish  →  Publish / Unpublish (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{slotId:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid classId, Guid slotId,
        [FromQuery] bool isPublished = true,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassId == classId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            slot.IsPublished = isPublished;
            slot.UpdatedAt = DateTime.UtcNow;
            slot.UpdatedBy = userId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = isPublished ? "Slot published." : "Slot unpublished.", slot.IsPublished });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred." });
        }
    }

    // ──────────────────────────────────────────
    // GET .../slots/{slotId}/scores  →  Score Assignment (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{slotId:guid}/scores")]
    public async Task<IActionResult> GetScores(
        Guid classId, Guid slotId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassId == classId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var problemIds = slot.ClassSlotProblems.Select(sp => sp.ProblemId).ToList();

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassId == classId && m.IsActive)
                .ToListAsync(ct);

            var result = new List<StudentSlotScoreResponse>();

            foreach (var m in members)
            {
                var subs = await _db.Submissions.AsNoTracking()
                    .Where(s => s.UserId == m.UserId && problemIds.Contains(s.ProblemId))
                    .ToListAsync(ct);

                var problemScores = slot.ClassSlotProblems.OrderBy(sp => sp.Ordinal).Select(sp =>
                {
                    var pSubs = subs.Where(s => s.ProblemId == sp.ProblemId).ToList();
                    var best = pSubs.Where(s => s.FinalScore.HasValue)
                        .OrderByDescending(s => s.FinalScore).FirstOrDefault();
                    return new ProblemScoreEntry(
                        sp.ProblemId,
                        sp.Problem.Title,
                        best?.VerdictCode,
                        best?.FinalScore,
                        pSubs.Count,
                        pSubs.Max(s => (DateTime?)s.CreatedAt));
                }).ToList();

                var total = problemScores.Where(p => p.Score.HasValue).Sum(p => p.Score!.Value);
                var solved = problemScores.Count(p => p.VerdictCode == "ac");

                result.Add(new StudentSlotScoreResponse(
                    m.UserId, m.User.DisplayName, m.User.AvatarUrl,
                    problemScores, total, solved));
            }

            return Ok(ApiResponse<List<StudentSlotScoreResponse>>.Ok(
                result.OrderByDescending(r => r.TotalScore).ToList(),
                "Scores fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching scores." });
        }
    }

    // ──────────────────────────────────────────
    // GET .../slots/{slotId}/submissions/{userId}  →  View Student's Submission (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{slotId:guid}/submissions/{userId:guid}")]
    public async Task<IActionResult> GetStudentSubmissions(
        Guid classId, Guid slotId, Guid userId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassId == classId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var problemIds = slot.ClassSlotProblems.Select(sp => sp.ProblemId).ToList();

            var submissions = await _db.Submissions.AsNoTracking()
                .Include(s => s.Problem)
                .Where(s => s.UserId == userId && problemIds.Contains(s.ProblemId))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);

            var result = new List<StudentSubmissionDetailResponse>();

            foreach (var s in submissions)
            {
                var results = await _db.Results.AsNoTracking()
                    .Where(r => r.SubmissionId == s.Id)
                    .OrderBy(r => r.Id)
                    .Select(r => new SubmissionResultEntry(
                        r.Id, r.StatusCode, r.RuntimeMs, r.MemoryKb,
                        r.CheckerMessage, r.Input, r.ExpectedOutput, r.ActualOutput))
                    .ToListAsync(ct);

                result.Add(new StudentSubmissionDetailResponse(
                    s.Id, s.ProblemId, s.Problem.Title,
                    s.VerdictCode, s.FinalScore, s.TimeMs, s.MemoryKb,
                    s.StatusCode, s.CreatedAt, results));
            }

            return Ok(ApiResponse<List<StudentSubmissionDetailResponse>>.Ok(
                result, "Submissions fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching submissions." });
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
