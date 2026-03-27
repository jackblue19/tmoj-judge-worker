using Asp.Versioning;
using Domain.Entities;
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
        [FromBody] CreateClassRequest req ,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.ClassCode))
                return BadRequest(new { Message = "ClassCode is required." });

            if ( !await _db.Subjects.AnyAsync(s => s.SubjectId == req.SubjectId , ct) )
                return BadRequest(new { Message = "Subject not found." });

            if ( !await _db.Semesters.AnyAsync(s => s.SemesterId == req.SemesterId , ct) )
                return BadRequest(new { Message = "Semester not found." });

            var codeNorm = req.ClassCode.Trim().ToUpperInvariant();
            if ( await _db.Classes.AnyAsync(c => c.ClassCode == codeNorm , ct) )
                return Conflict(new { Message = $"ClassCode '{codeNorm}' already exists." });

            var cls = new Domain.Entities.Class
            {
                SubjectId = req.SubjectId,
                ClassCode = codeNorm,
                Description = req.Description?.Trim(),
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                TeacherId = req.TeacherId,
                IsActive = true
            };

            _db.Classes.Add(cls);
            await _db.SaveChangesAsync(ct);

            // Create ClassSemester junction record
            _db.ClassSemesters.Add(new ClassSemester
            {
                ClassId = cls.ClassId,
                SemesterId = req.SemesterId
            });
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById) , new { id = cls.ClassId } ,
                ApiResponse<object>.Ok(new { cls.ClassId , cls.ClassCode } , "Class created successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while creating the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class  →  View All Class (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? semesterId ,
        [FromQuery] Guid? subjectId ,
        [FromQuery] string? search ,
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Classes.AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(c => c.Teacher)
                .Where(c => c.IsActive);

            if ( semesterId.HasValue )
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value));
            if ( subjectId.HasValue )
                query = query.Where(c => c.SubjectId == subjectId.Value);
            if ( !string.IsNullOrWhiteSpace(search) )
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var result = items.Select(c =>
            {
                var semester = c.ClassSemesters.FirstOrDefault()?.Semester;
                return new ClassResponse(
                    c.ClassId, c.ClassCode, c.Description,
                    c.StartDate, c.EndDate, c.IsActive, c.InviteCode,
                    c.InviteCodeExpiresAt,
                    c.CreatedAt, c.UpdatedAt,
                    new ClassSubjectInfo(c.Subject.SubjectId, c.Subject.Code, c.Subject.Name),
                    c.ClassSemesters.Select(cs => new ClassSemesterInfo(cs.Semester.SemesterId, cs.Semester.Code, cs.Semester.Name)).ToList(),
                    c.Teacher != null
                        ? new ClassTeacherInfo(c.Teacher.UserId , c.Teacher.DisplayName , c.Teacher.Email , c.Teacher.AvatarUrl)
                        : null ,
                    c.ClassMembers.Count(m => m.IsActive));
            }).ToList();

            return Ok(ApiResponse<ClassListResponse>.Ok(
                new ClassListResponse(result , totalCount) , "Classes fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching classes." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id , CancellationToken ct)
    {
        try
        {
            var c = await _db.Classes.AsNoTracking()
                .Include(x => x.Subject)
                .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(x => x.Teacher)
                .Include(x => x.ClassMembers)
                .FirstOrDefaultAsync(x => x.ClassId == id , ct);

            if ( c is null ) return NotFound(new { Message = "Class not found." });

            var dto = new ClassResponse(
                c.ClassId, c.ClassCode, c.Description,
                c.StartDate, c.EndDate, c.IsActive, c.InviteCode,
                c.InviteCodeExpiresAt,
                c.CreatedAt, c.UpdatedAt,
                new ClassSubjectInfo(c.Subject.SubjectId, c.Subject.Code, c.Subject.Name),
                c.ClassSemesters.Select(cs => new ClassSemesterInfo(cs.Semester.SemesterId, cs.Semester.Code, cs.Semester.Name)).ToList(),
                c.Teacher != null
                    ? new ClassTeacherInfo(c.Teacher.UserId , c.Teacher.DisplayName , c.Teacher.Email , c.Teacher.AvatarUrl)
                    : null ,
                c.ClassMembers.Count(m => m.IsActive));

            return Ok(ApiResponse<ClassResponse>.Ok(dto , "Class fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching the class." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{id}/teacher  →  Assign Teacher (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}/teacher")]
    public async Task<IActionResult> AssignTeacher(
        Guid id ,
        [FromBody] AssignTeacherRequest req ,
        CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            var teacher = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.TeacherId , ct);
            if ( teacher is null ) return BadRequest(new { Message = "Teacher user not found." });

            cls.TeacherId = req.TeacherId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Teacher assigned successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while assigning teacher." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/assign-teacher-role  →  Assign Teacher Role (Manager)
    // Uses User.RoleId (1:1 with Role) instead of UserRole M:N table
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost("assign-teacher-role")]
    public async Task<IActionResult> AssignTeacherRole(
        [FromBody] AssignTeacherRoleRequest req ,
        CancellationToken ct)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == req.UserId , ct);
            if ( user is null ) return NotFound(new { Message = "User not found." });

            var teacherRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "teacher" , ct);
            if ( teacherRole is null ) return StatusCode(500 , new { Message = "Teacher role not found in system." });

            if ( user.RoleId == teacherRole.RoleId )
                return Conflict(new { Message = "User already has the teacher role." });

            user.RoleId = teacherRole.RoleId;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Teacher role assigned successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while assigning teacher role." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/invite-code  →  Create Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{id:guid}/invite-code")]
    public async Task<IActionResult> CreateInviteCode(Guid id , CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if ( userId is null ) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            if ( cls.TeacherId != userId )
                return Forbid();

            // Generate 8-char alphanumeric code
            var code = GenerateInviteCode(8);
            while ( await _db.Classes.AnyAsync(c => c.InviteCode == code , ct) )
                code = GenerateInviteCode(8);

            var expiresAt = DateTime.UtcNow.AddMinutes(30);

            cls.InviteCode = code;
            cls.InviteCodeExpiresAt = expiresAt;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<InviteCodeResponse>.Ok(
                new InviteCodeResponse(cls.ClassId , code , expiresAt) , "Invite code created successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while creating invite code." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/invite-code  →  Close Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{id:guid}/invite-code")]
    public async Task<IActionResult> CloseInviteCode(Guid id , CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if ( userId is null ) return Unauthorized();

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            if ( cls.TeacherId != userId )
                return Forbid();

            if ( string.IsNullOrEmpty(cls.InviteCode) )
                return BadRequest(new { Message = "No active invite code to close." });

            cls.InviteCode = null;
            cls.InviteCodeExpiresAt = null;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new { cls.ClassId } , "Invite code closed successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while closing invite code." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/members  →  Add Student (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddStudent(
        Guid id ,
        [FromBody] AddStudentRequest req ,
        CancellationToken ct)
    {
        try
        {
            var teacherId = GetUserId();
            if ( teacherId is null ) return Unauthorized();

            var classSemester = await _db.ClassSemesters
                .Include(cs => cs.Class)
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == req.SemesterId , ct);

            if ( classSemester is null ) return NotFound(new { Message = "Class not linked to this semester." });
            if ( classSemester.Class.TeacherId != teacherId )
                return Forbid();

            // Find student
            User? student = null;
            if ( req.UserId.HasValue )
                student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId.Value , ct);
            else if ( !string.IsNullOrWhiteSpace(req.Email) )
                student = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant() , ct);

            if ( student is null ) return NotFound(new { Message = "Student not found." });

            var exists = await _db.ClassMembers
                .AnyAsync(m => m.ClassSemesterId == classSemester.Id && m.UserId == student.UserId , ct);
            if ( exists ) return Conflict(new { Message = "Student is already a member of this class in this semester." });

            _db.ClassMembers.Add(new ClassMember
            {
                ClassSemesterId = classSemester.Id ,
                UserId = student.UserId ,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student added successfully." , student.UserId });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while adding student." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/members/{userId}  →  Remove Student (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveStudent(
        Guid id , Guid userId , [FromQuery] Guid semesterId, CancellationToken ct)
    {
        try
        {
            var teacherId = GetUserId();
            if ( teacherId is null ) return Unauthorized();

            var classSemester = await _db.ClassSemesters
                .Include(cs => cs.Class)
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == semesterId, ct);
            
            if ( classSemester is null ) return NotFound(new { Message = "Class not linked to this semester." });
            if ( classSemester.Class.TeacherId != teacherId )
                return Forbid();

            var member = await _db.ClassMembers
                .FirstOrDefaultAsync(m => m.ClassSemesterId == classSemester.Id && m.UserId == userId , ct);
            if ( member is null ) return NotFound(new { Message = "Member not found in this class/semester." });

            member.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student removed successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while removing student." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{id}/members/{userId}/status → Update Member Status
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{id:guid}/members/{userId:guid}/status")]
    public async Task<IActionResult> UpdateMemberStatus(
        Guid id, Guid userId, [FromQuery] Guid semesterId, [FromBody] UpdateClassMemberStatusRequest req, CancellationToken ct)
    {
        try
        {
            var teacherId = GetUserId();
            if (teacherId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters
                .Include(cs => cs.Class)
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == semesterId, ct);
            
            if (classSemester is null) return NotFound(new { Message = "Class not linked to this semester." });
            if (classSemester.Class.TeacherId != teacherId) return Forbid();

            var member = await _db.ClassMembers.FirstOrDefaultAsync(m => m.ClassSemesterId == classSemester.Id && m.UserId == userId, ct);
            if (member is null) return NotFound(new { Message = "Member not found." });

            member.IsActive = req.IsActive;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Member status updated successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating status." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/join  →  Submit Invite Code (Student)
    // ──────────────────────────────────────────
    [HttpPost("join")]
    public async Task<IActionResult> JoinByInviteCode(
        [FromBody] JoinByCodeRequest req ,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if ( userId is null ) return Unauthorized();

            if ( string.IsNullOrWhiteSpace(req.InviteCode) )
                return BadRequest(new { Message = "Invite code is required." });

            var cls = await _db.Classes
                .Include(c => c.Subject)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
                .FirstOrDefaultAsync(c => c.InviteCode == req.InviteCode.Trim() && c.IsActive , ct);

            if ( cls is null ) return NotFound(new { Message = "Invalid or expired invite code." });

            // Check if invite code has expired
            if ( cls.InviteCodeExpiresAt.HasValue && cls.InviteCodeExpiresAt.Value < DateTime.UtcNow )
            {
                // Auto-clear expired invite code
                cls.InviteCode = null;
                cls.InviteCodeExpiresAt = null;
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { Message = "Invite code has expired." });
            }

            var exists = await _db.ClassMembers
                .AnyAsync(m => m.ClassSemesterId == cls.ClassSemesters.OrderByDescending(x => x.CreatedAt).First().Id && m.UserId == userId.Value , ct);
            if ( exists ) return Conflict(new { Message = "You are already a member of this class." });

            var latestSemester = cls.ClassSemesters.OrderByDescending(x => x.CreatedAt).First();

            _db.ClassMembers.Add(new ClassMember
            {
                ClassSemesterId = latestSemester.Id ,
                UserId = userId.Value ,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Joined class successfully." , cls.ClassId , cls });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while joining the class." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/members/me  →  Leave Class (Student)
    // ──────────────────────────────────────────
    [HttpDelete("{id:guid}/members/me")]
    public async Task<IActionResult> LeaveClass(Guid id , CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if ( userId is null ) return Unauthorized();

            var member = await _db.ClassMembers
                .FirstOrDefaultAsync(m => m.ClassSemester.ClassId == id && m.UserId == userId.Value && m.IsActive , ct);
            if ( member is null ) return NotFound(new { Message = "You are not an active member in any semester of this class." });

            member.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Left class successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while leaving the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/members/{userId}  →  View Student Information (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> GetStudentInfo(
        Guid id , Guid userId , CancellationToken ct)
    {
        try
        {
            var member = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.ClassSemester)
                .FirstOrDefaultAsync(m => m.ClassSemester.ClassId == id && m.UserId == userId && m.IsActive, ct);

            if ( member is null ) return NotFound(new { Message = "Member not found." });

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
                member.User.UserId ,
                member.User.FirstName ,
                member.User.LastName ,
                member.User.DisplayName ,
                member.User.Email ,
                member.User.AvatarUrl ,
                member.JoinedAt ,
                member.IsActive ,
                submissions.Count ,
                submissions.Count(s => s.VerdictCode == "ac") ,
                submissions.Max(s => (DateTime?) s.CreatedAt));

            return Ok(ApiResponse<StudentInfoResponse>.Ok(dto , "Student info fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching student info." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/members  →  List All Members
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid id , [FromQuery] Guid? semesterId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            var query = _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemester.ClassId == id && m.IsActive);

            if (semesterId.HasValue)
                query = query.Where(m => m.ClassSemester.SemesterId == semesterId.Value);

            var members = await query
                .OrderBy(m => m.JoinedAt)
                .Select(m => new ClassMemberResponse(
                    m.Id,
                    m.ClassSemester.ClassId,
                    m.UserId ,
                    m.User.DisplayName ,
                    m.User.Email ,
                    m.User.AvatarUrl ,
                    m.JoinedAt ,
                    m.IsActive))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<ClassMemberResponse>>.Ok(members , "Members fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching members." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/ranking  →  View Subject Ranking
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}/ranking")]
    public async Task<IActionResult> GetRanking(Guid id , [FromQuery] Guid? semesterId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            // Find ClassSemester
            var csQuery = _db.ClassSemesters.AsNoTracking().Where(cs => cs.ClassId == id);
            if (semesterId.HasValue)
                csQuery = csQuery.Where(cs => cs.SemesterId == semesterId.Value);
            
            var classSemester = await csQuery.FirstOrDefaultAsync(ct);
            if (classSemester is null) return NotFound(new { Message = "Class not found in specified semester." });

            // Get all problem ids in this class's slots
            var problemIds = await _db.ClassSlots.AsNoTracking()
                .Where(s => s.ClassId == id)
                .SelectMany(s => (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).Select(sp => sp.ProblemId))
                .Distinct()
                .ToListAsync(ct);

            // Get all active members for this class-semester
            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == classSemester.Id && m.IsActive)
                .ToListAsync(ct);

            var rankings = new List<ClassRankingEntry>();

            foreach ( var m in members )
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
                    0 , m.UserId , m.User.DisplayName , m.User.AvatarUrl ,
                    solved , totalScore , subs.Count));
            }

            // Assign ranks
            var ranked = rankings
                .OrderByDescending(r => r.TotalScore)
                .ThenByDescending(r => r.SolvedCount)
                .Select((r , i) => r with { Rank = i + 1 })
                .ToList();

            return Ok(ApiResponse<List<ClassRankingEntry>>.Ok(ranked , "Ranking fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching ranking." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/report/export  →  Export Mark Report (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{id:guid}/report/export")]
    public async Task<IActionResult> ExportMarkReport(Guid id , [FromQuery] Guid semesterId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking()
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });

            var classSemester = await _db.ClassSemesters.AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class-Semester not found." });

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == classSemester.Id && m.IsActive)
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
            foreach ( var slot in slots )
                sb.Append($",Slot{slot.SlotNo} - {slot.Title}");
            sb.AppendLine(",Total");

            int rowNum = 1;
            foreach ( var m in members )
            {
                sb.Append($"{rowNum},{m.UserId},{CsvEscape(m.User.DisplayName)},{m.User.Email}");
                decimal total = 0;
                foreach ( var slot in slots )
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

            return File(bytes , "text/csv; charset=utf-8" , fileName);
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while exporting the report." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{id}/students/export  →  Export Student List with Marks (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{id:guid}/students/export")]
    public async Task<IActionResult> ExportStudentsWithMarks(Guid id, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.AsNoTracking()
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            var classSemester = await _db.ClassSemesters.AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == semesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class-Semester not found." });

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == classSemester.Id && m.IsActive)
                .OrderBy(m => m.User.FirstName).ThenBy(m => m.User.LastName)
                .ToListAsync(ct);

            var problemIds = await _db.ClassSlots.AsNoTracking()
                .Where(s => s.ClassId == id)
                .SelectMany(s => (s.ClassSlotProblems ?? new List<ClassSlotProblem>()).Select(sp => sp.ProblemId))
                .Distinct()
                .ToListAsync(ct);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Student_Marks");

            var headers = new List<string> { "FullName", "Class", "Subject", "Mark", "MemberCode", "RollNumber" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            int row = 2;
            foreach (var m in members)
            {
                var subs = await _db.Submissions.AsNoTracking()
                    .Where(s => s.UserId == m.UserId && problemIds.Contains(s.ProblemId) && s.FinalScore.HasValue)
                    .GroupBy(s => s.ProblemId)
                    .Select(g => g.Max(s => s.FinalScore!.Value))
                    .ToListAsync(ct);
                
                var totalScore = subs.Sum();

                worksheet.Cell(row, 1).Value = $"{m.User.FirstName} {m.User.LastName}".Trim();
                worksheet.Cell(row, 2).Value = cls.ClassCode;
                worksheet.Cell(row, 3).Value = cls.Subject.Code;
                worksheet.Cell(row, 4).Value = totalScore;
                worksheet.Cell(row, 5).Value = m.User.MemberCode;
                worksheet.Cell(row, 6).Value = m.User.RollNumber;
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{cls.ClassCode}_StudentMarks_Export.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while exporting: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/semesters  →  Link Class to Semester (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost("{id:guid}/semesters")]
    public async Task<IActionResult> AddSemester(
        Guid id,
        [FromBody] AddClassSemesterRequest req,
        CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            if (!await _db.Semesters.AnyAsync(s => s.SemesterId == req.SemesterId, ct))
                return BadRequest(new { Message = "Semester not found." });

            var exists = await _db.ClassSemesters
                .AnyAsync(cs => cs.ClassId == id && cs.SemesterId == req.SemesterId, ct);
            if (exists) return Conflict(new { Message = "Class is already linked to this semester." });

            _db.ClassSemesters.Add(new ClassSemester
            {
                ClassId = id,
                SemesterId = req.SemesterId
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Class linked to semester successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while linking class to semester." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/semesters/{semesterId}  →  Unlink Class from Semester (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}/semesters/{semesterId:guid}")]
    public async Task<IActionResult> RemoveSemester(
        Guid id, Guid semesterId, CancellationToken ct)
    {
        try
        {
            var link = await _db.ClassSemesters
                .FirstOrDefaultAsync(cs => cs.ClassId == id && cs.SemesterId == semesterId, ct);
            if (link is null) return NotFound(new { Message = "Class-Semester link not found." });

            _db.ClassSemesters.Remove(link);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Class unlinked from semester successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while unlinking class from semester." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/import/template  →  Export Template Class (Admin/Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpGet("import/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string>
            {
                "ClassCode",
                "SubjectCode",
                "SemesterCode",
                "TeacherEmail",
                "Description",
                "StartDate",
                "EndDate"
            };

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            // Sample row
            worksheet.Cell(2, 1).Value = "SE1701";
            worksheet.Cell(2, 2).Value = "PRN211";
            worksheet.Cell(2, 3).Value = "FA23";
            worksheet.Cell(2, 4).Value = "teacher@domain.com";
            worksheet.Cell(2, 5).Value = "CSharp Programming";
            worksheet.Cell(2, 6).Value = "2023-09-01";
            worksheet.Cell(2, 7).Value = "2023-12-31";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Class_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/import  →  Import Class from Excel (Admin/Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost("import")]
    public async Task<IActionResult> ImportClasses(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is missing." });

        try
        {
            int successCount = 0;
            int failedCount = 0;
            var errors = new List<string>();
            int totalProcessed = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RowsUsed().Skip(1); // skip header

            var headerRow = worksheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;
            }

            var subjectsDict = await _db.Subjects.ToDictionaryAsync(s => s.Code.ToLowerInvariant(), s => s.SubjectId, ct);
            var semestersDict = await _db.Semesters.ToDictionaryAsync(s => s.Code.ToLowerInvariant(), s => s.SemesterId, ct);

            foreach (var row in rows)
            {
                totalProcessed++;
                try
                {
                    string classCodeRaw = GetCellString(row, headers, "ClassCode");
                    string subjectCodeRaw = GetCellString(row, headers, "SubjectCode");
                    string semesterCodeRaw = GetCellString(row, headers, "SemesterCode");
                    string teacherEmailRaw = GetCellString(row, headers, "TeacherEmail");
                    string descRaw = GetCellString(row, headers, "Description");
                    string startDateRaw = GetCellString(row, headers, "StartDate");
                    string endDateRaw = GetCellString(row, headers, "EndDate");

                    if (string.IsNullOrWhiteSpace(classCodeRaw) || string.IsNullOrWhiteSpace(subjectCodeRaw) || string.IsNullOrWhiteSpace(semesterCodeRaw))
                    {
                        errors.Add($"Row {row.RowNumber()}: Missing Required fields (ClassCode, SubjectCode, SemesterCode).");
                        failedCount++;
                        continue;
                    }

                    var classCode = classCodeRaw.Trim().ToUpperInvariant();
                    var subjectCode = subjectCodeRaw.Trim().ToLowerInvariant();
                    var semesterCode = semesterCodeRaw.Trim().ToLowerInvariant();

                    if (!subjectsDict.TryGetValue(subjectCode, out var subjectId))
                    {
                        errors.Add($"Row {row.RowNumber()}: Subject '{subjectCodeRaw}' not found.");
                        failedCount++;
                        continue;
                    }

                    if (!semestersDict.TryGetValue(semesterCode, out var semesterId))
                    {
                        errors.Add($"Row {row.RowNumber()}: Semester '{semesterCodeRaw}' not found.");
                        failedCount++;
                        continue;
                    }

                    Guid? teacherId = null;
                    if (!string.IsNullOrWhiteSpace(teacherEmailRaw))
                    {
                        var teacherEmail = teacherEmailRaw.Trim().ToLowerInvariant();
                        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Email == teacherEmail, ct);
                        if (teacher == null)
                        {
                            errors.Add($"Row {row.RowNumber()}: Teacher '{teacherEmailRaw}' not found.");
                            failedCount++;
                            continue;
                        }
                        teacherId = teacher.UserId;
                    }

                    DateOnly? startDate = null;
                    DateOnly? endDate = null;

                    if (!string.IsNullOrWhiteSpace(startDateRaw) && DateOnly.TryParse(startDateRaw, out var sDate))
                        startDate = sDate;

                    if (!string.IsNullOrWhiteSpace(endDateRaw) && DateOnly.TryParse(endDateRaw, out var eDate))
                        endDate = eDate;

                    // Check if class already exists — if so, just link to semester
                    var existingClass = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == classCode, ct);

                    if (existingClass != null)
                    {
                        // Class exists — link to semester if not already linked
                        var alreadyLinked = await _db.ClassSemesters
                            .AnyAsync(cs => cs.ClassId == existingClass.ClassId && cs.SemesterId == semesterId, ct);
                        if (!alreadyLinked)
                        {
                            _db.ClassSemesters.Add(new ClassSemester
                            {
                                ClassId = existingClass.ClassId,
                                SemesterId = semesterId
                            });
                            await _db.SaveChangesAsync(ct);
                        }
                        successCount++;
                        continue;
                    }

                    var cls = new Domain.Entities.Class
                    {
                        SubjectId = subjectId,
                        ClassCode = classCode,
                        Description = descRaw,
                        StartDate = startDate,
                        EndDate = endDate,
                        TeacherId = teacherId,
                        IsActive = true
                    };
                    _db.Classes.Add(cls);
                    await _db.SaveChangesAsync(ct);

                    // Create ClassSemester junction record
                    _db.ClassSemesters.Add(new ClassSemester
                    {
                        ClassId = cls.ClassId,
                        SemesterId = semesterId
                    });
                    await _db.SaveChangesAsync(ct);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                    failedCount++;
                }
            }

            var result = new
            {
                TotalProcessed = totalProcessed,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Errors = errors
            };

            return Ok(ApiResponse<object>.Ok(result, "Import processed with results."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/members/import/template  →  Excel Template for Members
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("members/import/template")]
    public IActionResult DownloadMemberImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "ClassCode", "SubjectCode", "SemesterCode", "RollNumber", "MemberCode" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            worksheet.Cell(2, 1).Value = "SE1701";
            worksheet.Cell(2, 2).Value = "PRN211";
            worksheet.Cell(2, 3).Value = "FA23";
            worksheet.Cell(2, 4).Value = "HE170001";
            worksheet.Cell(2, 5).Value = "M12345";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Member_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{id}/members/import  →  Import Members to Specific Class
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{id:guid}/members/import")]
    public async Task<IActionResult> ImportSpecificMembers(Guid id, [FromQuery] Guid semesterId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(new { Message = "File is missing." });
        var result = await ProcessMemberImport(file, id, semesterId, ct);
        return Ok(ApiResponse<ImportResultResponse>.Ok(result, "Import processed."));
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/members/import  →  Import Members by Codes
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("members/import")]
    public async Task<IActionResult> ImportMembersGeneric(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(new { Message = "File is missing." });
        var result = await ProcessMemberImport(file, null, null, ct);
        return Ok(ApiResponse<ImportResultResponse>.Ok(result, "Import processed."));
    }

    private async Task<ImportResultResponse> ProcessMemberImport(IFormFile file, Guid? fallbackClassId, Guid? fallbackSemesterId, CancellationToken ct)
    {
        int successCount = 0; int failedCount = 0; int totalProcessed = 0;
        var errors = new List<string>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1);
        var headers = worksheet.Row(1).CellsUsed().ToDictionary(c => c.GetValue<string>().Trim(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

        var semestersDict = await _db.Semesters.ToDictionaryAsync(s => s.Code.ToLowerInvariant(), s => s.SemesterId, ct);

        foreach (var row in rows)
        {
            totalProcessed++;
            try
            {
                string rNum = GetCellString(row, headers, "RollNumber");
                string mCode = GetCellString(row, headers, "MemberCode");

                if (string.IsNullOrWhiteSpace(rNum) && string.IsNullOrWhiteSpace(mCode))
                {
                    errors.Add($"Row {row.RowNumber()}: Student identifier missing.");
                    failedCount++; continue;
                }

                var student = await _db.Users.FirstOrDefaultAsync(u => (rNum != "" && u.RollNumber == rNum) || (mCode != "" && u.MemberCode == mCode), ct);
                if (student == null) { errors.Add($"Row {row.RowNumber()}: Student not found."); failedCount++; continue; }

                Guid targetClassId; Guid targetSemesterId;

                if (fallbackClassId.HasValue && fallbackSemesterId.HasValue)
                {
                    targetClassId = fallbackClassId.Value; targetSemesterId = fallbackSemesterId.Value;
                }
                else
                {
                    string cCode = GetCellString(row, headers, "ClassCode");
                    string sCode = GetCellString(row, headers, "SemesterCode");
                    if (string.IsNullOrWhiteSpace(cCode) || string.IsNullOrWhiteSpace(sCode))
                    {
                        errors.Add($"Row {row.RowNumber()}: Class/Semester code missing.");
                        failedCount++; continue;
                    }
                    if (!semestersDict.TryGetValue(sCode.ToLowerInvariant(), out targetSemesterId))
                    {
                        errors.Add($"Row {row.RowNumber()}: Semester '{sCode}' not found.");
                        failedCount++; continue;
                    }
                    var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == cCode.ToUpperInvariant(), ct);
                    if (cls == null) { errors.Add($"Row {row.RowNumber()}: Class '{cCode}' not found."); failedCount++; continue; }
                    targetClassId = cls.ClassId;
                }

                var classSemester = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.ClassId == targetClassId && cs.SemesterId == targetSemesterId, ct);
                if (classSemester == null)
                {
                    errors.Add($"Row {row.RowNumber()}: Class is not linked to this semester.");
                    failedCount++; continue;
                }

                var exists = await _db.ClassMembers.AnyAsync(m => m.ClassSemesterId == classSemester.Id && m.UserId == student.UserId, ct);
                if (!exists)
                {
                    _db.ClassMembers.Add(new ClassMember { ClassSemesterId = classSemester.Id, UserId = student.UserId, IsActive = true, JoinedAt = DateTime.UtcNow });
                    await _db.SaveChangesAsync(ct);
                }
                successCount++;
            }
            catch (Exception ex) { errors.Add($"Row {row.RowNumber()}: {ex.Message}"); failedCount++; }
        }
        return new ImportResultResponse(totalProcessed, successCount, failedCount, errors);
    }

    // ── Helpers ───────────────────────────────

    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string columnName)
    {
        if (headers.TryGetValue(columnName, out int colIdx))
        {
            try
            {
                var cell = row.Cell(colIdx);
                if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
                {
                    return cell.GetDateTime().ToString("yyyy-MM-dd");
                }
                return cell.GetString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return row.Cell(colIdx).GetValue<string>()?.Trim() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr , out var id) ? id : null;
    }

    private static string GenerateInviteCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string CsvEscape(string? value)
    {
        if ( string.IsNullOrEmpty(value) ) return "";
        if ( value.Contains(',') || value.Contains('"') || value.Contains('\n') )
            return $"\"{value.Replace("\"" , "\"\"")}\"";
        return value;
    }
}
