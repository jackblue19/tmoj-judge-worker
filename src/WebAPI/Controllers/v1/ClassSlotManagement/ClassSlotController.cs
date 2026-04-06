using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ClassSlotManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class-instance/{instanceId:guid}/slots")]
[Authorize]
public class ClassSlotController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ClassSlotController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // GET .../slots  →  List all slots for a class instance
    // ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid instanceId, CancellationToken ct)
    {
        try
        {
            var exists = await _db.ClassSemesters.AnyAsync(cs => cs.Id == instanceId, ct);
            if (!exists) return NotFound(new { Message = "Class instance not found." });

            var slots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .Where(s => s.ClassSemesterId == instanceId)
                .OrderBy(s => s.SlotNo)
                .ToListAsync(ct);

            var result = slots.Select(s => new ClassSlotResponse(
                s.Id, s.ClassSemesterId, s.SlotNo, s.Title, s.Description, s.Rules,
                s.OpenAt, s.DueAt, s.CloseAt, s.Mode, s.ContestId, s.IsPublished,
                s.CreatedAt, s.UpdatedAt,
                (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).OrderBy(sp => sp.Ordinal).Select(sp => new SlotProblemResponse(
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
    Guid instanceId,
    [FromBody] CreateClassSlotRequest req,
    CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });
            
            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { Message = "Title is required." });

            var mode = req.Mode?.ToLowerInvariant() ?? "problemset";
            if (mode is not ("problemset" or "contest"))
                return BadRequest(new { Message = "Mode must be 'problemset' or 'contest'." });

            // Check unique slot_no within this instance
            if (await _db.ClassSlots.AnyAsync(s => s.ClassSemesterId == instanceId && s.SlotNo == req.SlotNo, ct))
                return Conflict(new { Message = $"SlotNo {req.SlotNo} already exists in this class instance." });

            var slot = new ClassSlot
            {
                ClassSemesterId = instanceId,
                SlotNo = req.SlotNo,
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                Rules = req.Rules?.Trim(),
                OpenAt = req.OpenAt?.ToUniversalTime(),
                DueAt = req.DueAt?.ToUniversalTime(),
                CloseAt = req.CloseAt?.ToUniversalTime(),
                Mode = mode,
                IsPublished = false,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            _db.ClassSlots.Add(slot);
            await _db.SaveChangesAsync(ct);

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

            return CreatedAtAction(nameof(GetAll), new { instanceId, version = "1.0" },
                new { Message = "Assignment created successfully.", slot.Id, slot.SlotNo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the assignment.", Detail = ex.Message });
        }
    } 

    // ──────────────────────────────────────────
    // PUT .../slots/{slotId}/due-date  →  Set Due Date (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{slotId:guid}/due-date")]
    public async Task<IActionResult> SetDueDate(
        Guid instanceId, Guid slotId,
        [FromBody] SetDueDateRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });
            
            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found in this instance." });

            slot.DueAt = req.DueAt;
            if (req.CloseAt.HasValue) slot.CloseAt = req.CloseAt.Value;
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
    public async Task<IActionResult> TogglePublish(
     Guid instanceId,
     Guid slotId,
     CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });
            
            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);

            if (slot is null) return NotFound(new { Message = "Slot not found." });

            slot.IsPublished = !slot.IsPublished;
            slot.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                Message = slot.IsPublished ? "Slot published." : "Slot unpublished.",
                IsPublished = slot.IsPublished
            });
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
        Guid instanceId, Guid slotId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
            var problemIds = slotProblems.Select(sp => sp.ProblemId).ToList();

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == instanceId && m.IsActive)
                .ToListAsync(ct);

            var result = new List<StudentSlotScoreResponse>();

            foreach (var m in members)
            {
                var subs = problemIds.Count > 0
                    ? await _db.Submissions.AsNoTracking()
                        .Where(s => s.UserId == m.UserId && problemIds.Contains(s.ProblemId))
                        .ToListAsync(ct)
                    : new List<Submission>();

                var problemScores = slotProblems.OrderBy(sp => sp.Ordinal).Select(sp =>
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
                        pSubs.Any() ? pSubs.Max(s => (DateTime?)s.CreatedAt) : null);
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
        Guid instanceId, Guid slotId, Guid userId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
            var problemIds = slotProblems.Select(sp => sp.ProblemId).ToList();

            var submissions = problemIds.Count > 0
                ? await _db.Submissions.AsNoTracking()
                    .Include(s => s.Problem)
                    .Where(s => s.UserId == userId && problemIds.Contains(s.ProblemId))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(ct)
                : new List<Submission>();

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

    // ──────────────────────────────────────────
    // PUT .../slots/{slotId}  →  Update Slot details (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{slotId:guid}")]
    public async Task<IActionResult> Update(
        Guid instanceId, Guid slotId,
        [FromBody] UpdateClassSlotRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            if (req.Title is not null) slot.Title = req.Title.Trim();
            if (req.Description is not null) slot.Description = req.Description.Trim();
            if (req.Rules is not null) slot.Rules = req.Rules.Trim();
            if (req.OpenAt.HasValue) slot.OpenAt = req.OpenAt.Value.ToUniversalTime();
            if (req.DueAt.HasValue) slot.DueAt = req.DueAt.Value.ToUniversalTime();
            if (req.CloseAt.HasValue) slot.CloseAt = req.CloseAt.Value.ToUniversalTime();
            if (req.IsPublished.HasValue) slot.IsPublished = req.IsPublished.Value;
            slot.UpdatedBy = userId;

            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Slot updated successfully.", slot.Id });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the slot." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE .../slots/{slotId}  →  Delete Slot (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{slotId:guid}")]
    public async Task<IActionResult> Delete(
        Guid instanceId, Guid slotId,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .Include(s => s.ClassSlotProblems)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            // Remove associated problems first
            if (slot.ClassSlotProblems?.Any() == true)
                _db.ClassSlotProblems.RemoveRange(slot.ClassSlotProblems);

            _db.ClassSlots.Remove(slot);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Slot deleted successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the slot." });
        }
    }

    // ──────────────────────────────────────────
    // POST .../slots/{slotId}/problems  →  Add problems to slot (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{slotId:guid}/problems")]
    public async Task<IActionResult> AddProblems(
        Guid instanceId, Guid slotId,
        [FromBody] List<SlotProblemItem> problems,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .Include(s => s.ClassSlotProblems)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            if (problems is not { Count: > 0 })
                return BadRequest(new { Message = "At least one problem is required." });

            var existingProblemIds = (slot.ClassSlotProblems ?? new List<ClassSlotProblem>())
                .Select(sp => sp.ProblemId).ToHashSet();

            var added = 0;
            foreach (var p in problems)
            {
                if (existingProblemIds.Contains(p.ProblemId))
                    continue; // skip duplicates

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
                added++;
            }

            slot.UpdatedBy = userId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = $"{added} problem(s) added to slot.", Added = added });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while adding problems." });
        }
    }

    // ──────────────────────────────────────────
    // PUT .../slots/{slotId}/problems  →  Update problems in slot (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{slotId:guid}/problems")]
    public async Task<IActionResult> UpdateProblems(
        Guid instanceId, Guid slotId,
        [FromBody] List<SlotProblemItem> problems,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .Include(s => s.ClassSlotProblems)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            if (problems is not { Count: > 0 })
                return BadRequest(new { Message = "Problems payload is required." });

            var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
            var updated = 0;

            foreach (var p in problems)
            {
                var existing = slotProblems.FirstOrDefault(sp => sp.ProblemId == p.ProblemId);
                if (existing != null)
                {
                    existing.Ordinal = p.Ordinal;
                    existing.Points = p.Points;
                    existing.IsRequired = p.IsRequired;
                    updated++;
                }
            }

            if (updated > 0)
            {
                slot.UpdatedBy = userId;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { Message = $"{updated} problem(s) updated successfully.", Updated = updated });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating problems." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE .../slots/{slotId}/problems  →  Remove problems from slot (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{slotId:guid}/problems")]
    public async Task<IActionResult> RemoveProblems(
        Guid instanceId, Guid slotId,
        [FromBody] List<Guid> problemIds,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == instanceId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var slot = await _db.ClassSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == instanceId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            if (problemIds is not { Count: > 0 })
                return BadRequest(new { Message = "At least one problem ID is required." });

            var toRemove = await _db.ClassSlotProblems
                .Where(sp => sp.SlotId == slotId && problemIds.Contains(sp.ProblemId))
                .ToListAsync(ct);

            if (toRemove.Count == 0)
                return NotFound(new { Message = "None of the specified problems were found in this slot." });

            _db.ClassSlotProblems.RemoveRange(toRemove);
            slot.UpdatedBy = userId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = $"{toRemove.Count} problem(s) removed from slot.", Removed = toRemove.Count });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while removing problems." });
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
