using Asp.Versioning;
using Domain.Entities;
using Class = Domain.Entities.Class;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ClassManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClassController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ClassController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // POST api/v1/class  →  Create Class (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClassRequest req,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.ClassCode))
                return BadRequest(new { Message = "ClassCode is required." });

            if (!await _db.Subjects.AnyAsync(s => s.SubjectId == req.SubjectId, ct))
                return BadRequest(new { Message = "Subject not found." });

            if (!await _db.Semesters.AnyAsync(s => s.SemesterId == req.SemesterId, ct))
                return BadRequest(new { Message = "Semester not found." });

            var codeNorm = req.ClassCode.Trim().ToUpperInvariant();
            if (await _db.Classes.AnyAsync(c => c.ClassCode == codeNorm, ct))
                return Conflict(new { Message = $"ClassCode '{codeNorm}' already exists." });

            var cls = new Domain.Entities.Class
            {
                SubjectId = req.SubjectId,
                SemesterId = req.SemesterId,
                ClassCode = codeNorm,
                Description = req.Description?.Trim(),
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                TeacherId = req.TeacherId,
                IsActive = true
            };

            _db.Classes.Add(cls);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = cls.ClassId },
                ApiResponse<object>.Ok(new { cls.ClassId, cls.ClassCode }, "Class created successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class  →  View All Class (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Classes.AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .Include(c => c.Teacher)
                .Where(c => c.IsActive);

            if (semesterId.HasValue)
                query = query.Where(c => c.SemesterId == semesterId.Value);
            if (subjectId.HasValue)
                query = query.Where(c => c.SubjectId == subjectId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClassResponse(
                    c.ClassId, c.ClassCode, c.Description,
                    c.StartDate, c.EndDate, c.IsActive, c.InviteCode,
                    c.InviteCodeExpiresAt,
                    c.CreatedAt, c.UpdatedAt,
                    new ClassSubjectInfo(c.Subject.SubjectId, c.Subject.Code, c.Subject.Name),
                    new ClassSemesterInfo(c.Semester.SemesterId, c.Semester.Code, c.Semester.Name),
                    c.Teacher != null
                        ? new ClassTeacherInfo(c.Teacher.UserId, c.Teacher.DisplayName, c.Teacher.Email, c.Teacher.AvatarUrl)
                        : null,
                    c.ClassMembers.Count(m => m.IsActive)))
                .ToListAsync(ct);

            return Ok(ApiResponse<ClassListResponse>.Ok(
                new ClassListResponse(items, totalCount), "Classes fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching classes." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var c = await _db.Classes.AsNoTracking()
                .Include(x => x.Subject)
                .Include(x => x.Semester)
                .Include(x => x.Teacher)
                .Include(x => x.ClassMembers)
                .FirstOrDefaultAsync(x => x.ClassId == id, ct);

            if (c is null) return NotFound(new { Message = "Class not found." });

            var dto = new ClassResponse(
                c.ClassId, c.ClassCode, c.Description,
                c.StartDate, c.EndDate, c.IsActive, c.InviteCode,
                c.InviteCodeExpiresAt,
                c.CreatedAt, c.UpdatedAt,
                new ClassSubjectInfo(c.Subject.SubjectId, c.Subject.Code, c.Subject.Name),
                new ClassSemesterInfo(c.Semester.SemesterId, c.Semester.Code, c.Semester.Name),
                c.Teacher != null
                    ? new ClassTeacherInfo(c.Teacher.UserId, c.Teacher.DisplayName, c.Teacher.Email, c.Teacher.AvatarUrl)
                    : null,
                c.ClassMembers.Count(m => m.IsActive));

            return Ok(ApiResponse<ClassResponse>.Ok(dto, "Class fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the class." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{id}/teacher  →  Assign Teacher (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}/teacher")]
    public async Task<IActionResult> AssignTeacher(
        Guid id,
        [FromBody] AssignTeacherRequest req,
        CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var teacher = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.TeacherId, ct);
            if (teacher is null) return BadRequest(new { Message = "Teacher user not found." });

            cls.TeacherId = req.TeacherId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Teacher assigned successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while assigning teacher." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/assign-teacher-role  →  Assign Teacher Role (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost("assign-teacher-role")]
    public async Task<IActionResult> AssignTeacherRole(
        [FromBody] AssignTeacherRoleRequest req,
        CancellationToken ct)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId, ct);
            if (user is null) return NotFound(new { Message = "User not found." });

            var teacherRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "teacher", ct);
            if (teacherRole is null) return StatusCode(500, new { Message = "Teacher role not found in system." });

            var alreadyHas = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == req.UserId && ur.RoleId == teacherRole.RoleId, ct);
            if (alreadyHas) return Conflict(new { Message = "User already has the teacher role." });

            _db.UserRoles.Add(new UserRole
            {
                UserId = req.UserId,
                RoleId = teacherRole.RoleId
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Teacher role assigned successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while assigning teacher role." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/invite-code  →  Create Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{id:guid}/invite-code")]
    public async Task<IActionResult> CreateInviteCode(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            if (cls.TeacherId != userId)
                return Forbid();

            // Generate 8-char alphanumeric code
            var code = GenerateInviteCode(8);
            while (await _db.Classes.AnyAsync(c => c.InviteCode == code, ct))
                code = GenerateInviteCode(8);

            var expiresAt = DateTime.UtcNow.AddMinutes(30);

            cls.InviteCode = code;
            cls.InviteCodeExpiresAt = expiresAt;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<InviteCodeResponse>.Ok(
                new InviteCodeResponse(cls.ClassId, code, expiresAt), "Invite code created successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating invite code." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/invite-code  →  Close Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{id:guid}/invite-code")]
    public async Task<IActionResult> CloseInviteCode(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            if (cls.TeacherId != userId)
                return Forbid();

            if (string.IsNullOrEmpty(cls.InviteCode))
                return BadRequest(new { Message = "No active invite code to close." });

            cls.InviteCode = null;
            cls.InviteCodeExpiresAt = null;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new { cls.ClassId }, "Invite code closed successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while closing invite code." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/members  →  Add Student (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddStudent(
        Guid id,
        [FromBody] AddStudentRequest req,
        CancellationToken ct)
    {
        try
        {
            var teacherId = GetUserId();
            if (teacherId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != teacherId)
                return Forbid();

            // Find student
            User? student = null;
            if (req.UserId.HasValue)
                student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId.Value, ct);
            else if (!string.IsNullOrWhiteSpace(req.Email))
                student = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

            if (student is null) return NotFound(new { Message = "Student not found." });

            var exists = await _db.ClassMembers
                .AnyAsync(m => m.ClassId == id && m.UserId == student.UserId, ct);
            if (exists) return Conflict(new { Message = "Student is already a member of this class." });

            _db.ClassMembers.Add(new ClassMember
            {
                ClassId = id,
                UserId = student.UserId,
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student added successfully.", student.UserId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while adding student." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/members/{userId}  →  Remove Student (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveStudent(
        Guid id, Guid userId, CancellationToken ct)
    {
        try
        {
            var teacherId = GetUserId();
            if (teacherId is null) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });
            if (cls.TeacherId != teacherId)
                return Forbid();

            var member = await _db.ClassMembers
                .FirstOrDefaultAsync(m => m.ClassId == id && m.UserId == userId, ct);
            if (member is null) return NotFound(new { Message = "Member not found." });

            member.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student removed successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while removing student." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/join  →  Submit Invite Code (Student)
    // ──────────────────────────────────────────
    [HttpPost("join")]
    public async Task<IActionResult> JoinByInviteCode(
        [FromBody] JoinByCodeRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.InviteCode))
                return BadRequest(new { Message = "Invite code is required." });

            var cls = await _db.Classes
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.InviteCode == req.InviteCode.Trim() && c.IsActive, ct);

            if (cls is null) return NotFound(new { Message = "Invalid or expired invite code." });

            // Check if invite code has expired
            if (cls.InviteCodeExpiresAt.HasValue && cls.InviteCodeExpiresAt.Value < DateTime.UtcNow)
            {
                // Auto-clear expired invite code
                cls.InviteCode = null;
                cls.InviteCodeExpiresAt = null;
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { Message = "Invite code has expired." });
            }

            var exists = await _db.ClassMembers
                .AnyAsync(m => m.ClassId == cls.ClassId && m.UserId == userId.Value, ct);
            if (exists) return Conflict(new { Message = "You are already a member of this class." });

            _db.ClassMembers.Add(new ClassMember
            {
                ClassId = cls.ClassId,
                UserId = userId.Value,
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Joined class successfully.", cls.ClassId });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while joining the class." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/members/me  →  Leave Class (Student)
    // ──────────────────────────────────────────
    [HttpDelete("{id:guid}/members/me")]
    public async Task<IActionResult> LeaveClass(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var member = await _db.ClassMembers
                .FirstOrDefaultAsync(m => m.ClassId == id && m.UserId == userId.Value && m.IsActive, ct);
            if (member is null) return NotFound(new { Message = "You are not an active member of this class." });

            member.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Left class successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while leaving the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/members/{userId}  →  View Student Information (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> GetStudentInfo(
        Guid id, Guid userId, CancellationToken ct)
    {
        try
        {
            var member = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ClassId == id && m.UserId == userId, ct);

            if (member is null) return NotFound(new { Message = "Member not found." });

            // Get class slot problem ids for this class
            var slotProblemIds = await _db.ClassSlots.AsNoTracking()
                .Where(s => s.ClassId == id)
                .SelectMany(s => (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).Select(sp => sp.ProblemId))
                .Distinct()
                .ToListAsync(ct);

            var submissions = await _db.Submissions.AsNoTracking()
                .Where(s => s.UserId == userId && slotProblemIds.Contains(s.ProblemId))
                .ToListAsync(ct);

            var dto = new StudentInfoResponse(
                member.User.UserId,
                member.User.FirstName,
                member.User.LastName,
                member.User.DisplayName,
                member.User.Email,
                member.User.AvatarUrl,
                member.JoinedAt,
                member.IsActive,
                submissions.Count,
                submissions.Count(s => s.VerdictCode == "ac"),
                submissions.Max(s => (DateTime?)s.CreatedAt));

            return Ok(ApiResponse<StudentInfoResponse>.Ok(dto, "Student info fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching student info." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/members  →  List All Members
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid id, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassId == id && m.IsActive)
                .OrderBy(m => m.JoinedAt)
                .Select(m => new ClassMemberResponse(
                    m.User.UserId,
                    m.User.DisplayName,
                    m.User.Email,
                    m.User.AvatarUrl,
                    m.JoinedAt,
                    m.IsActive))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<ClassMemberResponse>>.Ok(members, "Members fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching members." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/ranking  →  View Subject Ranking
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}/ranking")]
    public async Task<IActionResult> GetRanking(Guid id, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            // Get all problem ids in this class's slots
            var problemIds = await _db.ClassSlots.AsNoTracking()
                .Where(s => s.ClassId == id)
                .SelectMany(s => (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).Select(sp => sp.ProblemId))
                .Distinct()
                .ToListAsync(ct);

            // Get all active members
            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassId == id && m.IsActive)
                .ToListAsync(ct);

            var rankings = new List<ClassRankingEntry>();

            foreach (var m in members)
            {
                var subs = await _db.Submissions.AsNoTracking()
                    .Where(s => s.UserId == m.UserId && problemIds.Contains(s.ProblemId))
                    .ToListAsync(ct);

                var solved = subs.Where(s => s.VerdictCode == "ac")
                    .Select(s => s.ProblemId).Distinct().Count();
                var totalScore = subs.Where(s => s.FinalScore.HasValue)
                    .GroupBy(s => s.ProblemId)
                    .Sum(g => g.Max(s => s.FinalScore!.Value));

                rankings.Add(new ClassRankingEntry(
                    0, m.UserId, m.User.DisplayName, m.User.AvatarUrl,
                    solved, totalScore, subs.Count));
            }

            // Assign ranks
            var ranked = rankings
                .OrderByDescending(r => r.TotalScore)
                .ThenByDescending(r => r.SolvedCount)
                .Select((r, i) => r with { Rank = i + 1 })
                .ToList();

            return Ok(ApiResponse<List<ClassRankingEntry>>.Ok(ranked, "Ranking fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching ranking." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/report/export  →  Export Mark Report (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{id:guid}/report/export")]
    public async Task<IActionResult> ExportMarkReport(Guid id, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking()
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassId == id && m.IsActive)
                .OrderBy(m => m.User.DisplayName)
                .ToListAsync(ct);

            var slots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.ClassSlotProblems)
                .Where(s => s.ClassId == id)
                .OrderBy(s => s.SlotNo)
                .ToListAsync(ct);

            // Build CSV
            var sb = new StringBuilder();

            // Header
            sb.Append("No,UserId,DisplayName,Email");
            foreach (var slot in slots)
                sb.Append($",Slot{slot.SlotNo} - {slot.Title}");
            sb.AppendLine(",Total");

            int rowNum = 1;
            foreach (var m in members)
            {
                sb.Append($"{rowNum},{m.UserId},{CsvEscape(m.User.DisplayName)},{m.User.Email}");
                decimal total = 0;
                foreach (var slot in slots)
                {
                    var problemIds = (slot.ClassSlotProblems ?? new List<ClassSlotProblem>()).Select(sp => sp.ProblemId).ToList();
                    var bestScores = await _db.Submissions.AsNoTracking()
                        .Where(s => s.UserId == m.UserId && problemIds.Contains(s.ProblemId)
                                 && s.FinalScore.HasValue)
                        .GroupBy(s => s.ProblemId)
                        .Select(g => g.Max(s => s.FinalScore!.Value))
                        .ToListAsync(ct);
                    var slotScore = bestScores.Sum();
                    total += slotScore;
                    sb.Append($",{slotScore}");
                }
                sb.AppendLine($",{total}");
                rowNum++;
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"{cls.ClassCode}_MarkReport_{DateTime.UtcNow:yyyyMMdd}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while exporting the report." });
        }
    }

    // ── Helpers ───────────────────────────────

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : null;
    }

    private static string GenerateInviteCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
