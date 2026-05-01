using Asp.Versioning;
using Application.UseCases.ClassSlots.Commands;
using Application.UseCases.ClassSlots.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Common;
using MediatR;
using Application.UseCases.ClassSlots.Queries;

namespace WebAPI.Controllers.v1.ClassSlotManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class-semester/{semesterId:guid}/slots")]
[Authorize]
public class ClassSlotController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IMediator _mediator;

    public ClassSlotController(TmojDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    // ──────────────────────────────────────────
    // GET .../slots  →  List all slots for a class instance
    // ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid semesterId, CancellationToken ct)
    {
        try
        {
            var exists = await _db.ClassSemesters.AnyAsync(cs => cs.Id == semesterId, ct);
            if (!exists) return NotFound(new { Message = "Class instance not found." });

            var slots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .Where(s => s.ClassSemesterId == semesterId)
                .OrderBy(s => s.SlotNo)
                .ToListAsync(ct);

            var result = slots.Select(s => new ClassSlotDto(
                s.Id, s.ClassSemesterId, s.SlotNo, s.Title, s.Description, s.Rules,
                s.OpenAt, s.DueAt, s.CloseAt, s.Mode, s.IsPublished,
                s.CreatedAt, s.UpdatedAt,
                (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).OrderBy(sp => sp.Ordinal).Select(sp => new SlotProblemDto(
                    sp.ProblemId, sp.Problem.Title, sp.Problem.Slug,
                    sp.Ordinal, sp.Points, sp.IsRequired)).ToList()
            )).ToList();

            return Ok(ApiResponse<List<ClassSlotDto>>.Ok(result, "Slots fetched successfully"));
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
    Guid semesterId,
    [FromBody] CreateClassSlotBody req,
    CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId)
                return Forbid();

            var slotId = await _mediator.Send(
                new CreateClassSlotCommand(
                    semesterId, req.SlotNo, req.Title, req.Description, req.Rules,
                    req.OpenAt, req.DueAt, req.CloseAt, req.Mode, req.Problems), ct);

            return CreatedAtAction(nameof(GetAll), new { semesterId, version = "1.0" },
                new { Message = "Assignment created successfully.", Id = slotId, SlotNo = req.SlotNo });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
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
        Guid semesterId, Guid slotId,
        [FromBody] SetDueDateBody req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            await _mediator.Send(
                new SetClassSlotDueDateCommand(semesterId, slotId, req.DueAt, req.CloseAt), ct);

            return Ok(new { Message = "Due date updated successfully.", DueAt = req.DueAt, CloseAt = req.CloseAt });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Slot not found." });
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
     Guid semesterId,
     Guid slotId,
     CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            var isPublished = await _mediator.Send(
                new ToggleClassSlotPublishCommand(semesterId, slotId), ct);

            return Ok(new
            {
                Message = isPublished ? "Slot published." : "Slot unpublished.",
                IsPublished = isPublished
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Slot not found." });
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
        Guid semesterId, Guid slotId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == semesterId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
            var problemIds = slotProblems.Select(sp => sp.ProblemId).ToList();

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == semesterId && m.IsActive)
                .ToListAsync(ct);

            var result = new List<StudentSlotScoreDto>();

            foreach (var m in members)
            {
                // Lấy submission kèm số test case pass/total để tính %
                var subs = problemIds.Count > 0
                    ? await _db.Submissions.AsNoTracking()
                        .Where(s => s.UserId == m.UserId
                                    && s.ClassSlotId == slotId
                                    && problemIds.Contains(s.ProblemId))
                        .Select(s => new
                        {
                            s.Id,
                            s.ProblemId,
                            s.VerdictCode,
                            s.CreatedAt,
                            Total = _db.Results.Count(r => r.SubmissionId == s.Id && r.TestcaseId != null),
                            Passed = _db.Results.Count(r => r.SubmissionId == s.Id && r.TestcaseId != null && r.StatusCode == "ac")
                        })
                        .ToListAsync(ct)
                    : new();

                var problemScores = slotProblems.OrderBy(sp => sp.Ordinal).Select(sp =>
                {
                    var pSubs = subs.Where(s => s.ProblemId == sp.ProblemId).ToList();

                    // Tính điểm = (passed / total) * sp.Points cho mỗi submission, lấy max
                    decimal? bestScore = null;
                    string? bestVerdict = null;
                    DateTime? lastSubmittedAt = null;
                    int problemPoints = sp.Points ?? 0;

                    foreach (var s in pSubs)
                    {
                        if (s.Total > 0)
                        {
                            var pct = (decimal)s.Passed / s.Total;
                            var score = Math.Round(pct * problemPoints, 2);
                            if (bestScore is null || score > bestScore)
                            {
                                bestScore = score;
                                bestVerdict = s.VerdictCode;
                            }
                        }
                        if (lastSubmittedAt is null || s.CreatedAt > lastSubmittedAt)
                            lastSubmittedAt = s.CreatedAt;
                    }

                    return new ProblemScoreDto(
                        sp.ProblemId,
                        sp.Problem.Title,
                        bestVerdict,
                        bestScore,
                        pSubs.Count,
                        lastSubmittedAt);
                }).ToList();

                var total = problemScores.Where(p => p.Score.HasValue).Sum(p => p.Score!.Value);
                var solved = problemScores.Count(p => p.VerdictCode == "ac");

                result.Add(new StudentSlotScoreDto(
                    m.UserId, m.User.DisplayName, m.User.AvatarUrl,
                    problemScores, total, solved));
            }

            return Ok(ApiResponse<List<StudentSlotScoreDto>>.Ok(
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
        Guid semesterId, Guid slotId, Guid userId, CancellationToken ct)
    {
        try
        {
            var slot = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems!).ThenInclude(sp => sp.Problem)
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ClassSemesterId == semesterId, ct);
            if (slot is null) return NotFound(new { Message = "Slot not found." });

            var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
            var problemIds = slotProblems.Select(sp => sp.ProblemId).ToList();

            var submissions = problemIds.Count > 0
                ? await _db.Submissions.AsNoTracking()
                    .Include(s => s.Problem)
                    .Where(s => s.UserId == userId
                                && s.ClassSlotId == slotId
                                && problemIds.Contains(s.ProblemId))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(ct)
                : new List<Submission>();

            var result = new List<StudentSubmissionDetailDto>();

            foreach (var s in submissions)
            {
                var results = await _db.Results.AsNoTracking()
                    .Where(r => r.SubmissionId == s.Id)
                    .OrderBy(r => r.Id)
                    .Select(r => new SubmissionResultDto(
                        r.Id, r.StatusCode, r.RuntimeMs, r.MemoryKb,
                        r.CheckerMessage, r.Input, r.ExpectedOutput, r.ActualOutput))
                    .ToListAsync(ct);

                result.Add(new StudentSubmissionDetailDto(
                    s.Id, s.ProblemId, s.Problem.Title,
                    s.VerdictCode, s.FinalScore, s.TimeMs, s.MemoryKb,
                    s.StatusCode, s.CreatedAt, results));
            }

            return Ok(ApiResponse<List<StudentSubmissionDetailDto>>.Ok(
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
        Guid semesterId, Guid slotId,
        [FromBody] UpdateClassSlotBody req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            await _mediator.Send(
                new UpdateClassSlotCommand(semesterId, slotId, req.Title, req.Description, req.Rules,
                    req.OpenAt, req.DueAt, req.CloseAt, req.IsPublished), ct);

            return Ok(new { Message = "Slot updated successfully.", Id = slotId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Slot not found." });
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
        Guid semesterId, Guid slotId,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            await _mediator.Send(
                new DeleteClassSlotCommand(semesterId, slotId), ct);

            return Ok(new { Message = "Slot deleted successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Slot not found." });
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
        Guid semesterId, Guid slotId,
        [FromBody] List<SlotProblemBody> problems,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            if (problems is not { Count: > 0 })
                return BadRequest(new { Message = "At least one problem is required." });

            var problemItems = problems.Select(p =>
                new SlotProblemItem(p.ProblemId, p.Ordinal ?? 0, p.Points, p.IsRequired))
                .ToList();

            var added = await _mediator.Send(
                new AddSlotProblemsCommand(semesterId, slotId, problemItems), ct);

            return Ok(new { Message = $"{added} problem(s) added to slot.", Added = added });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
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
        Guid semesterId, Guid slotId,
        [FromBody] List<SlotProblemBody> problems,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            if (problems is not { Count: > 0 })
                return BadRequest(new { Message = "Problems payload is required." });

            var problemItems = problems.Select(p =>
                new SlotProblemUpdateItem(p.ProblemId, p.Ordinal ?? 0, p.Points, p.IsRequired))
                .ToList();

            var updated = await _mediator.Send(
                new UpdateSlotProblemsCommand(semesterId, slotId, problemItems), ct);

            return Ok(new { Message = $"{updated} problem(s) updated successfully.", Updated = updated });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
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
        Guid semesterId, Guid slotId,
        [FromBody] List<Guid> problemIds,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });

            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            if (problemIds is not { Count: > 0 })
                return BadRequest(new { Message = "At least one problem ID is required." });

            var removed = await _mediator.Send(
                new RemoveSlotProblemsCommand(semesterId, slotId, problemIds), ct);

            return Ok(new { Message = $"{removed} problem(s) removed from slot.", Removed = removed });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
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

    // ──────────────────────────────────────────
    // GET api/v{version}/class-semester/{semesterId}/slots/{classSlotId}/rankings
    // Student rankings for a specific class slot
    // ──────────────────────────────────────────
    [HttpGet("{classSlotId:guid}/rankings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClassSlotRankings(
        Guid classSlotId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetClassSlotRankingsQuery { ClassSlotId = classSlotId },
                ct);

            return Ok(ApiResponse<ClassSlotRankingDto>.Ok(
                result,
                "Fetched class slot rankings successfully"
            ));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Class slot not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching rankings." });
        }
    }
}

public record CreateClassSlotBody(
    int SlotNo,
    string Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    string Mode,
    List<CreateSlotProblemItem>? Problems);

public record UpdateClassSlotBody(
    string? Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    bool? IsPublished);

public record SetDueDateBody(DateTime DueAt, DateTime? CloseAt);

public record SlotProblemBody(Guid ProblemId, int? Ordinal, int? Points, bool IsRequired);
