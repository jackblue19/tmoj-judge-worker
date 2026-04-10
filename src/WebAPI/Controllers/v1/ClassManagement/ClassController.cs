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
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

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

            var codeNorm = req.ClassCode.Trim().ToUpperInvariant();
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == codeNorm , ct);
            if ( cls is null )
            {
                cls = new Domain.Entities.Class
                {
                    ClassCode = codeNorm,
                    IsActive = true
                };
                _db.Classes.Add(cls);
                await _db.SaveChangesAsync(ct);
            }
            else if ( !cls.IsActive )
            {
                cls.IsActive = true;
                _db.Classes.Update(cls);
                await _db.SaveChangesAsync(ct);
            }

            // Create ClassSemester junction record (Course Instance)
            var exists = await _db.ClassSemesters.AnyAsync(cs => 
                cs.ClassId == cls.ClassId && cs.SemesterId == req.SemesterId && cs.SubjectId == req.SubjectId, ct);
            
            if (exists) return Conflict(new { Message = "This class is already enrolled in this subject and semester." });

            var instance = new ClassSemester
            {
                ClassId = cls.ClassId,
                SemesterId = req.SemesterId,
                SubjectId = req.SubjectId,
                TeacherId = req.TeacherId,
                CreatedAt = DateTime.UtcNow
            };
            _db.ClassSemesters.Add(instance);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById) , new { id = cls.ClassId } ,
                ApiResponse<object>.Ok(new { cls.ClassId, instance.Id, cls.ClassCode } , "Class instance created successfully"));
        }
        catch ( Exception ex )
        {
            return StatusCode(500 , new { Message = "An error occurred while creating the class." , Detail = ex.InnerException?.Message ?? ex.Message });
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
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
                .Where(c => c.IsActive);

            if ( semesterId.HasValue )
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value));
            if ( subjectId.HasValue )
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.SubjectId == subjectId.Value));
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
                // Filter instances theo semesterId/subjectId để chỉ trả về đúng instance phù hợp
                var filteredSemesters = c.ClassSemesters
                    .Where(cs => cs.Semester != null && cs.Subject != null);

                if (semesterId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SemesterId == semesterId.Value);
                if (subjectId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SubjectId == subjectId.Value);

                var instancesList = filteredSemesters.ToList();

                var instances = instancesList
                    .Select(cs => new ClassInstanceInfo(
                        cs.Id,
                        cs.Semester.SemesterId, cs.Semester.Code,
                        cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                        cs.Semester.StartAt, cs.Semester.EndAt,
                        cs.InviteCode, cs.InviteCodeExpiresAt, cs.CreatedAt,
                        cs.Teacher != null ? new ClassTeacherInfo(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
                        cs.ClassMembers.Count(m => m.IsActive)))
                    .ToList();

                return new ClassResponse(
                    c.ClassId, c.ClassCode, c.IsActive,
                    c.CreatedAt, c.UpdatedAt,
                    instances,
                    instancesList.SelectMany(cs => cs.ClassMembers).Where(m => m.IsActive).Select(m => m.UserId).Distinct().Count());
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
                .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Subject)
                .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Teacher)
                .Include(x => x.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
                .FirstOrDefaultAsync(x => x.ClassId == id , ct);

            if ( c is null ) return NotFound(new { Message = "Class not found." });

            var instances = c.ClassSemesters
                .Where(cs => cs.Semester != null && cs.Subject != null)
                .Select(cs => new ClassInstanceInfo(
                    cs.Id,
                    cs.Semester.SemesterId, cs.Semester.Code,
                    cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                    cs.Semester.StartAt, cs.Semester.EndAt,
                    cs.InviteCode, cs.InviteCodeExpiresAt, cs.CreatedAt,
                    cs.Teacher != null ? new ClassTeacherInfo(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
                    cs.ClassMembers.Count(m => m.IsActive)))
                .ToList();

            var dto = new ClassResponse(
                c.ClassId, c.ClassCode, c.IsActive,
                c.CreatedAt, c.UpdatedAt,
                instances,
                c.ClassSemesters.SelectMany(cs => cs.ClassMembers).Where(m => m.IsActive).Select(m => m.UserId).Distinct().Count());

            return Ok(ApiResponse<ClassResponse>.Ok(dto , "Class fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/my-classes/student  →  Get classes where current user is a student
    // ──────────────────────────────────────────
    [HttpGet("my-classes/student")]
    public async Task<IActionResult> GetMyClassesAsStudent(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            // Get ClassSemester IDs where the user is an active member
            var memberQuery = _db.ClassMembers.AsNoTracking()
                .Where(m => m.UserId == userId.Value && m.IsActive)
                .Select(m => m.ClassSemesterId);

            var query = _db.Classes.AsNoTracking()
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
                .Where(c => c.IsActive && c.ClassSemesters.Any(cs => memberQuery.Contains(cs.Id)));

            if (semesterId.HasValue)
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value && memberQuery.Contains(cs.Id)));
            if (subjectId.HasValue)
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.SubjectId == subjectId.Value && memberQuery.Contains(cs.Id)));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var memberCsIds = await _db.ClassMembers.AsNoTracking()
                .Where(m => m.UserId == userId.Value && m.IsActive)
                .Select(m => m.ClassSemesterId)
                .ToListAsync(ct);

            var result = items.Select(c =>
            {
                // Only show instances the student belongs to
                var filteredSemesters = c.ClassSemesters
                    .Where(cs => cs.Semester != null && cs.Subject != null && memberCsIds.Contains(cs.Id));

                if (semesterId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SemesterId == semesterId.Value);
                if (subjectId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SubjectId == subjectId.Value);

                var instancesList = filteredSemesters.ToList();

                var instances = instancesList
                    .Select(cs => new ClassInstanceInfo(
                        cs.Id,
                        cs.Semester.SemesterId, cs.Semester.Code,
                        cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                        cs.Semester.StartAt, cs.Semester.EndAt,
                        null, null, cs.CreatedAt, // hide invite code from students
                        cs.Teacher != null ? new ClassTeacherInfo(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
                        cs.ClassMembers.Count(m => m.IsActive)))
                    .ToList();

                return new ClassResponse(
                    c.ClassId, c.ClassCode, c.IsActive,
                    c.CreatedAt, c.UpdatedAt,
                    instances,
                    instancesList.SelectMany(cs => cs.ClassMembers).Where(m => m.IsActive).Select(m => m.UserId).Distinct().Count());
            }).ToList();

            return Ok(ApiResponse<ClassListResponse>.Ok(
                new ClassListResponse(result, totalCount), "Student classes fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching student classes." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/my-classes/teacher  →  Get classes where current user is the teacher
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("my-classes/teacher")]
    public async Task<IActionResult> GetMyClassesAsTeacher(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var query = _db.Classes.AsNoTracking()
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
                .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
                .Where(c => c.IsActive && c.ClassSemesters.Any(cs => cs.TeacherId == userId.Value));

            if (semesterId.HasValue)
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.TeacherId == userId.Value && cs.SemesterId == semesterId.Value));
            if (subjectId.HasValue)
                query = query.Where(c => c.ClassSemesters.Any(cs => cs.TeacherId == userId.Value && cs.SubjectId == subjectId.Value));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var result = items.Select(c =>
            {
                // Only show instances where the current user is the teacher
                var filteredSemesters = c.ClassSemesters
                    .Where(cs => cs.Semester != null && cs.Subject != null && cs.TeacherId == userId.Value);

                if (semesterId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SemesterId == semesterId.Value);
                if (subjectId.HasValue)
                    filteredSemesters = filteredSemesters.Where(cs => cs.SubjectId == subjectId.Value);

                var instancesList = filteredSemesters.ToList();

                var instances = instancesList
                    .Select(cs => new ClassInstanceInfo(
                        cs.Id,
                        cs.Semester.SemesterId, cs.Semester.Code,
                        cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                        cs.Semester.StartAt, cs.Semester.EndAt,
                        cs.InviteCode, cs.InviteCodeExpiresAt, cs.CreatedAt,
                        cs.Teacher != null ? new ClassTeacherInfo(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
                        cs.ClassMembers.Count(m => m.IsActive)))
                    .ToList();

                return new ClassResponse(
                    c.ClassId, c.ClassCode, c.IsActive,
                    c.CreatedAt, c.UpdatedAt,
                    instances,
                    instancesList.SelectMany(cs => cs.ClassMembers).Where(m => m.IsActive).Select(m => m.UserId).Distinct().Count());
            }).ToList();

            return Ok(ApiResponse<ClassListResponse>.Ok(
                new ClassListResponse(result, totalCount), "Teacher classes fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching teacher classes." });
        }
    }

    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id ,
        [FromBody] UpdateClassRequest req ,
        CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == id , ct);
            if ( cls is null ) return NotFound(new { Message = "Class not found." });
 
            if ( req.IsActive.HasValue ) cls.IsActive = req.IsActive.Value;
 
            cls.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
 
            return Ok(ApiResponse<object>.Ok(new { cls.ClassId } , "Class updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the class." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}  →  Soft-Delete Class (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClass(Guid id, CancellationToken ct)
    {
        try
        {
            var cls = await _db.Classes
                .Include(c => c.ClassSemesters)
                .FirstOrDefaultAsync(c => c.ClassId == id, ct);
            if (cls is null) return NotFound(new { Message = "Class not found." });

            // Soft-delete: deactivate the class
            cls.IsActive = false;
            cls.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new { cls.ClassId }, "Class deleted (deactivated) successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the class." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/assign-teacher-role  →  Assign Teacher Role (Manager)
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

            var instance = await _db.ClassSemesters
                .Include(cs => cs.Class)
                .FirstOrDefaultAsync(cs => cs.InviteCode == req.InviteCode.Trim(), ct);

            if (instance is null) return NotFound(new { Message = "Invalid or expired invite code." });

            if (instance.InviteCodeExpiresAt.HasValue && instance.InviteCodeExpiresAt.Value < DateTime.UtcNow)
            {
                instance.InviteCode = null;
                instance.InviteCodeExpiresAt = null;
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { Message = "Invite code has expired." });
            }

            var already = await _db.ClassMembers.AnyAsync(m => m.ClassSemesterId == instance.Id && m.UserId == userId.Value, ct);
            if (!already)
            {
                _db.ClassMembers.Add(new ClassMember
                {
                    ClassSemesterId = instance.Id,
                    UserId = userId.Value,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { Message = "Joined class instance successfully.", instance.ClassId, instanceId = instance.Id });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while joining the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/export  →  Export Class List by Semester (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpGet("export")]
    public async Task<IActionResult> ExportClasses(
        [FromQuery] Guid semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var semester = await _db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId, ct);
            if (semester is null) return NotFound(new { Message = "Semester not found." });

            var query = _db.ClassSemesters.AsNoTracking()
                .Include(cs => cs.Class)
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.ClassMembers)
                .Where(cs => cs.SemesterId == semesterId);

            if (subjectId.HasValue)
                query = query.Where(cs => cs.SubjectId == subjectId.Value);

            var instances = await query.OrderBy(cs => cs.Class.ClassCode).ToListAsync(ct);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Classes");

            var headers = new List<string> { "ClassCode", "SubjectCode", "SubjectName", "SemesterCode", "TeacherEmail", "TeacherName", "MemberCount" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            int row = 2;
            foreach (var cs in instances)
            {
                worksheet.Cell(row, 1).Value = cs.Class?.ClassCode ?? "N/A";
                worksheet.Cell(row, 2).Value = cs.Subject?.Code ?? "N/A";
                worksheet.Cell(row, 3).Value = cs.Subject?.Name ?? "N/A";
                worksheet.Cell(row, 4).Value = semester.Code;
                worksheet.Cell(row, 5).Value = cs.Teacher?.Email ?? "";
                worksheet.Cell(row, 6).Value = cs.Teacher?.DisplayName ?? "";
                worksheet.Cell(row, 7).Value = cs.ClassMembers.Count(m => m.IsActive);
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Classes_{semester.Code}_Export.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while exporting: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/students/export  →  Export All Students by Semester (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("students/export")]
    public async Task<IActionResult> ExportStudentsBySemester(
        [FromQuery] Guid semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var semester = await _db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId, ct);
            if (semester is null) return NotFound(new { Message = "Semester not found." });

            var query = _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.Class)
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.Subject)
                .Where(m => m.IsActive && m.ClassSemester.SemesterId == semesterId);

            if (subjectId.HasValue)
                query = query.Where(m => m.ClassSemester.SubjectId == subjectId.Value);

            var members = await query
                .OrderBy(m => m.ClassSemester.Class.ClassCode)
                .ThenBy(m => m.User.DisplayName)
                .ToListAsync(ct);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Students");

            var headers = new List<string> { "ClassCode", "SubjectCode", "FullName", "Email", "RollNumber", "MemberCode", "JoinedAt" };
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
                worksheet.Cell(row, 1).Value = m.ClassSemester?.Class?.ClassCode ?? "N/A";
                worksheet.Cell(row, 2).Value = m.ClassSemester?.Subject?.Code ?? "N/A";
                worksheet.Cell(row, 3).Value = $"{m.User.FirstName} {m.User.LastName}".Trim();
                worksheet.Cell(row, 4).Value = m.User.Email;
                worksheet.Cell(row, 5).Value = m.User.RollNumber;
                worksheet.Cell(row, 6).Value = m.User.MemberCode;
                worksheet.Cell(row, 7).Value = m.JoinedAt.ToString("yyyy-MM-dd");
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Students_{semester.Code}_Export.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
                .AnyAsync(cs => cs.ClassId == id && cs.SemesterId == req.SemesterId && cs.SubjectId == req.SubjectId, ct);
            if (exists) return Conflict(new { Message = "Class is already linked to this semester and subject." });

            _db.ClassSemesters.Add(new ClassSemester
            {
                ClassId = id,
                SemesterId = req.SemesterId,
                SubjectId = req.SubjectId,
                TeacherId = req.TeacherId,
                CreatedAt = DateTime.UtcNow
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
    // DELETE api/v1/class/{id}/semesters/{classSemesterId}  →  Delete Class-Semester instance (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}/semesters/{classSemesterId:guid}")]
    public async Task<IActionResult> RemoveSemester(
        Guid id, Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var link = await _db.ClassSemesters
                .Include(cs => cs.ClassMembers)
                .Include(cs => cs.ClassSlots)
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId && cs.ClassId == id, ct);
            if (link is null) return NotFound(new { Message = "Class-Semester instance not found." });

            // Remove related members and slots first
            _db.ClassMembers.RemoveRange(link.ClassMembers);
            _db.ClassSlots.RemoveRange(link.ClassSlots);
            _db.ClassSemesters.Remove(link);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Class-Semester instance deleted successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting class semester instance." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{id}/semesters/{classSemesterId}  →  Update Class-Semester instance (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}/semesters/{classSemesterId:guid}")]
    public async Task<IActionResult> UpdateSemester(
        Guid id, Guid classSemesterId,
        [FromBody] UpdateClassSemesterRequest req,
        CancellationToken ct)
    {
        try
        {
            var link = await _db.ClassSemesters
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId && cs.ClassId == id, ct);
            if (link is null) return NotFound(new { Message = "Class-Semester instance not found." });

            // Validate & apply ClassId
            if (req.ClassId.HasValue)
            {
                if (!await _db.Classes.AnyAsync(c => c.ClassId == req.ClassId.Value, ct))
                    return BadRequest(new { Message = "Class not found." });
                link.ClassId = req.ClassId.Value;
            }

            // Validate & apply SemesterId
            if (req.SemesterId.HasValue)
            {
                if (!await _db.Semesters.AnyAsync(s => s.SemesterId == req.SemesterId.Value, ct))
                    return BadRequest(new { Message = "Semester not found." });
                link.SemesterId = req.SemesterId.Value;
            }

            // Validate & apply SubjectId
            if (req.SubjectId.HasValue)
            {
                if (!await _db.Subjects.AnyAsync(s => s.SubjectId == req.SubjectId.Value, ct))
                    return BadRequest(new { Message = "Subject not found." });
                link.SubjectId = req.SubjectId.Value;
            }

            // Validate & apply TeacherId (supports both setting and clearing)
            if (req.TeacherId.HasValue)
            {
                if (req.TeacherId.Value == Guid.Empty)
                {
                    // Allow clearing the teacher by sending empty GUID
                    link.TeacherId = null;
                }
                else
                {
                    if (!await _db.Users.AnyAsync(u => u.UserId == req.TeacherId.Value, ct))
                        return BadRequest(new { Message = "Teacher user not found." });
                    link.TeacherId = req.TeacherId.Value;
                }
                // Explicitly mark TeacherId as modified to ensure EF tracks the change
                _db.Entry(link).Property(x => x.TeacherId).IsModified = true;
            }

            // Check duplicate combination (same class + semester + subject must be unique)
            var duplicate = await _db.ClassSemesters
                .AnyAsync(cs => cs.Id != classSemesterId
                    && cs.ClassId == link.ClassId
                    && cs.SemesterId == link.SemesterId
                    && cs.SubjectId == link.SubjectId, ct);
            if (duplicate)
                return Conflict(new { Message = "A class-semester instance with this combination already exists." });

            // Ensure EF detects the entity as modified
            _db.Entry(link).State = EntityState.Modified;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(
                new { link.Id, link.ClassId, link.SemesterId, link.SubjectId, link.TeacherId },
                "Class-Semester instance updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating class semester instance." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/export/template  →  Export Template Class (Admin/Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpGet("export/template")]
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
                "TeacherEmail"
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

                    // Check if class already exists — if so, just link to semester
                    var existingClass = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == classCode, ct);

                    if (existingClass != null)
                    {
                        // Class exists — link to semester if not already linked
                        var alreadyLinked = await _db.ClassSemesters
                            .AnyAsync(cs => cs.ClassId == existingClass.ClassId && cs.SemesterId == semesterId && cs.SubjectId == subjectId, ct);
                        if (!alreadyLinked)
                        {
                            _db.ClassSemesters.Add(new ClassSemester
                            {
                                ClassId = existingClass.ClassId,
                                SemesterId = semesterId,
                                SubjectId = subjectId,
                                TeacherId = teacherId,
                                CreatedAt = DateTime.UtcNow
                            });
                            await _db.SaveChangesAsync(ct);
                        }
                        successCount++;
                        continue;
                    }

                    var cls = new Domain.Entities.Class
                    {
                        ClassCode = classCode,
                        IsActive = true
                    };
                    _db.Classes.Add(cls);
                    await _db.SaveChangesAsync(ct);

                    // Create ClassSemester junction record
                    _db.ClassSemesters.Add(new ClassSemester
                    {
                        ClassId = cls.ClassId,
                        SemesterId = semesterId,
                        SubjectId = subjectId,
                        TeacherId = teacherId,
                        CreatedAt = DateTime.UtcNow
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
    // GET api/v1/class/{classSemesterId}/students/import/template  →  Download Student Import Template (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/students/import/template")]
    public async Task<IActionResult> DownloadStudentImportTemplate(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking()
                .Include(cs => cs.Class)
                .Include(cs => cs.Semester)
                .Include(cs => cs.Subject)
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);

            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            // Teacher can only access their own class
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "Email", "RollNumber", "MemberCode", "FirstName", "LastName" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            // Sample row
            worksheet.Cell(2, 1).Value = "student@domain.com";
            worksheet.Cell(2, 2).Value = "SE170001";
            worksheet.Cell(2, 3).Value = "HE170001";
            worksheet.Cell(2, 4).Value = "Nguyen";
            worksheet.Cell(2, 5).Value = "Van A";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Student_Import_Template_{instance.Class?.ClassCode}_{instance.Semester?.Code}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/students/import  →  Import Students from Excel (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/students/import")]
    public async Task<IActionResult> ImportStudents(Guid classSemesterId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is missing." });

        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters
                .Include(cs => cs.Class)
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);

            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

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

            // Get student role
            var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student", ct);

            foreach (var row in rows)
            {
                totalProcessed++;
                try
                {
                    string emailRaw = GetCellString(row, headers, "Email");
                    string rollNumberRaw = GetCellString(row, headers, "RollNumber");
                    string memberCodeRaw = GetCellString(row, headers, "MemberCode");
                    string firstNameRaw = GetCellString(row, headers, "FirstName");
                    string lastNameRaw = GetCellString(row, headers, "LastName");

                    if (string.IsNullOrWhiteSpace(emailRaw))
                    {
                        errors.Add($"Row {row.RowNumber()}: Email is required.");
                        failedCount++;
                        continue;
                    }

                    var email = emailRaw.Trim().ToLowerInvariant();
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

                    if (user == null)
                    {
                        // Create user account
                        user = new User
                        {
                            Email = email,
                            Username = email.Split('@')[0],
                            FirstName = firstNameRaw?.Trim() ?? "",
                            LastName = lastNameRaw?.Trim() ?? "",
                            DisplayName = $"{firstNameRaw?.Trim()} {lastNameRaw?.Trim()}".Trim(),
                            RollNumber = rollNumberRaw?.Trim(),
                            MemberCode = memberCodeRaw?.Trim(),
                            RoleId = studentRole?.RoleId,
                            EmailVerified = false,
                            Status = true,
                            LanguagePreference = "vi",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Users.Add(user);
                        await _db.SaveChangesAsync(ct);
                    }
                    else
                    {
                        // Update RollNumber/MemberCode if provided and not set
                        bool updated = false;
                        if (!string.IsNullOrWhiteSpace(rollNumberRaw) && string.IsNullOrWhiteSpace(user.RollNumber))
                        { user.RollNumber = rollNumberRaw.Trim(); updated = true; }
                        if (!string.IsNullOrWhiteSpace(memberCodeRaw) && string.IsNullOrWhiteSpace(user.MemberCode))
                        { user.MemberCode = memberCodeRaw.Trim(); updated = true; }
                        if (updated) await _db.SaveChangesAsync(ct);
                    }

                    // Check if already a member
                    var existing = await _db.ClassMembers
                        .FirstOrDefaultAsync(m => m.ClassSemesterId == classSemesterId && m.UserId == user.UserId, ct);

                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            await _db.SaveChangesAsync(ct);
                        }
                    }
                    else
                    {
                        _db.ClassMembers.Add(new ClassMember
                        {
                            ClassSemesterId = classSemesterId,
                            UserId = user.UserId,
                            IsActive = true,
                            JoinedAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync(ct);
                    }

                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                    failedCount++;
                }
            }

            return Ok(ApiResponse<ImportResultResponse>.Ok(
                new ImportResultResponse(totalProcessed, successCount, failedCount, errors),
                "Student import processed."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing students: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students  →  Get all students in a Class Instance (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/students")]
    public async Task<IActionResult> GetStudents(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == classSemesterId)
                .OrderBy(m => m.User.DisplayName)
                .Select(m => new ClassMemberResponse(
                    m.Id,
                    m.ClassSemesterId,
                    m.UserId,
                    m.User.DisplayName,
                    m.User.Email,
                    m.User.AvatarUrl,
                    m.JoinedAt,
                    m.IsActive))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<ClassMemberResponse>>.Ok(members, "Students fetched successfully."));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching students." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students/export  →  Export Students of Class Instance (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/students/export")]
    public async Task<IActionResult> ExportStudentsByClassInstance(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking()
                .Include(cs => cs.Class)
                .Include(cs => cs.Semester)
                .Include(cs => cs.Subject)
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);

            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            var members = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ClassSemesterId == classSemesterId && m.IsActive)
                .OrderBy(m => m.User.DisplayName)
                .ToListAsync(ct);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Students");

            var headers = new List<string> { "No", "Email", "RollNumber", "MemberCode", "FirstName", "LastName", "DisplayName", "JoinedAt" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            int row = 2;
            int no = 1;
            foreach (var m in members)
            {
                worksheet.Cell(row, 1).Value = no++;
                worksheet.Cell(row, 2).Value = m.User.Email;
                worksheet.Cell(row, 3).Value = m.User.RollNumber ?? "";
                worksheet.Cell(row, 4).Value = m.User.MemberCode ?? "";
                worksheet.Cell(row, 5).Value = m.User.FirstName;
                worksheet.Cell(row, 6).Value = m.User.LastName;
                worksheet.Cell(row, 7).Value = m.User.DisplayName ?? "";
                worksheet.Cell(row, 8).Value = m.JoinedAt.ToString("yyyy-MM-dd");
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Students_{instance.Class?.ClassCode}_{instance.Semester?.Code}_{instance.Subject?.Code}_Export.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while exporting students: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/invite-code  →  Get current Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> GetInviteCode(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking()
                .Include(cs => cs.Class)
                .Include(cs => cs.Semester)
                .Include(cs => cs.Subject)
                .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            // Check if invite code exists and is still valid
            if (string.IsNullOrEmpty(instance.InviteCode))
                return Ok(ApiResponse<object>.Ok(new
                {
                    instance.Id,
                    ClassCode = instance.Class?.ClassCode,
                    SemesterCode = instance.Semester?.Code,
                    SubjectCode = instance.Subject?.Code,
                    InviteCode = (string?)null,
                    ExpiresAt = (DateTime?)null,
                    IsActive = false
                }, "No active invite code."));

            var isExpired = instance.InviteCodeExpiresAt.HasValue && instance.InviteCodeExpiresAt.Value < DateTime.UtcNow;

            return Ok(ApiResponse<object>.Ok(new
            {
                instance.Id,
                ClassCode = instance.Class?.ClassCode,
                SemesterCode = instance.Semester?.Code,
                SubjectCode = instance.Subject?.Code,
                instance.InviteCode,
                ExpiresAt = instance.InviteCodeExpiresAt,
                IsActive = !isExpired
            }, isExpired ? "Invite code has expired." : "Invite code is active."));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching invite code." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/invite-code  →  Generate Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> GenerateInviteCode(Guid classSemesterId, [FromBody] GenerateInviteCodeRequest req, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            int minutes = req.MinutesValid > 15 ? 15 : (req.MinutesValid <= 0 ? 15 : req.MinutesValid);

            instance.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            instance.InviteCodeExpiresAt = DateTime.UtcNow.AddMinutes(minutes);

            await _db.SaveChangesAsync(ct);

            return Ok(new InviteCodeResponse(instance.Id, instance.InviteCode, instance.InviteCodeExpiresAt.Value));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating invite code: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{classSemesterId}/invite-code  →  Cancel Invite Code (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> CancelInviteCode(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            instance.InviteCode = null;
            instance.InviteCodeExpiresAt = null;

            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Invite code cancelled successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while cancelling invite code." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/students/manual  →  Teacher Manually Adds Student
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/students/manual")]
    public async Task<IActionResult> AddStudentManually(Guid classSemesterId, [FromBody] AddStudentManuallyRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RollNumber) && string.IsNullOrWhiteSpace(req.MemberCode))
            return BadRequest(new { Message = "Must provide either RollNumber or MemberCode." });

        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(req.RollNumber))
                query = query.Where(u => u.RollNumber == req.RollNumber.Trim());
            else if (!string.IsNullOrWhiteSpace(req.MemberCode))
                query = query.Where(u => u.MemberCode == req.MemberCode.Trim());

            var student = await query.FirstOrDefaultAsync(ct);
            if (student is null) return NotFound(new { Message = "Student not found in the system." });

            var existing = await _db.ClassMembers.FirstOrDefaultAsync(m => m.ClassSemesterId == classSemesterId && m.UserId == student.UserId, ct);
            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    await _db.SaveChangesAsync(ct);
                    return Ok(new { Message = "Student added (reactivated) successfully.", StudentId = student.UserId });
                }
                return Conflict(new { Message = "Student is already an active member of this class." });
            }

            _db.ClassMembers.Add(new ClassMember
            {
                ClassSemesterId = classSemesterId,
                UserId = student.UserId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student added successfully.", StudentId = student.UserId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while adding student manually: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{classSemesterId}/students/{studentId}  →  Update Student Status in Class (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{classSemesterId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> UpdateStudentStatus(Guid classSemesterId, Guid studentId, [FromBody] UpdateClassMemberStatusRequest req, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            var member = await _db.ClassMembers.FirstOrDefaultAsync(m => m.ClassSemesterId == classSemesterId && m.UserId == studentId, ct);
            if (member is null) return NotFound(new { Message = "Student is not in this class." });

            member.IsActive = req.IsActive;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student status updated successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating student." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{classSemesterId}/students/{studentId}  →  Remove Student from Class (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{classSemesterId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> RemoveStudent(Guid classSemesterId, Guid studentId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value)
                return Forbid();

            var member = await _db.ClassMembers.FirstOrDefaultAsync(m => m.ClassSemesterId == classSemesterId && m.UserId == studentId, ct);
            if (member is null) return NotFound(new { Message = "Student is not in this class." });

            _db.ClassMembers.Remove(member);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Student removed from class successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while removing student." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/contests  →  List contests in a class instance
    // ──────────────────────────────────────────
    [HttpGet("{classSemesterId:guid}/contests")]
    public async Task<IActionResult> GetAllContests(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var exists = await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct);
            if (!exists) return NotFound(new { Message = "Class instance not found." });

            var contestSlots = await _db.ClassSlots.AsNoTracking()
                .Include(s => s.Contest).ThenInclude(c => c!.ContestProblems)
                .Include(s => s.Contest).ThenInclude(c => c!.ContestTeams)
                .Where(s => s.ClassSemesterId == classSemesterId && s.Mode == "contest" && s.ContestId != null)
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
    // POST api/v1/class/{classSemesterId}/contests  →  Create Class's Contest (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/contests")]
    public async Task<IActionResult> CreateContest(
        Guid classSemesterId,
        [FromBody] CreateClassContestRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });
            
            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

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
                .Where(s => s.ClassSemesterId == classSemesterId)
                .MaxAsync(s => (int?)s.SlotNo, ct) ?? 0) + 1;

            var slot = new ClassSlot
            {
                ClassSemesterId = classSemesterId,
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

            return CreatedAtAction(nameof(GetContestById), new { classSemesterId, contestId = contest.Id, version = "1.0" },
                new { Message = "Contest created successfully.", contest.Id, SlotId = slot.Id });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the contest." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/contests/{contestId}  →  View Contest (Authenticated)
    // ──────────────────────────────────────────
    [HttpGet("{classSemesterId:guid}/contests/{contestId:guid}")]
    public async Task<IActionResult> GetContestById(
        Guid classSemesterId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            // Verify contest belongs to this class instance
            var slot = await _db.ClassSlots.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class instance." });

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
                contest.Id, classSemesterId, slot.Id,
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
    // PUT api/v1/class/{classSemesterId}/contests/{contestId}/extend  →  Extend Contest's Time (Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{classSemesterId:guid}/contests/{contestId:guid}/extend")]
    public async Task<IActionResult> ExtendContestTime(
        Guid classSemesterId, Guid contestId,
        [FromBody] ExtendContestRequest req,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var classSemester = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (classSemester is null) return NotFound(new { Message = "Class instance not found." });
            
            var isAdmin = User.IsInRole("admin") || User.IsInRole("manager");
            if (!isAdmin && classSemester.TeacherId != userId) return Forbid();

            // Verify contest belongs to class instance
            var slot = await _db.ClassSlots.FirstOrDefaultAsync(
                s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class instance." });

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
    // POST api/v1/class/{classSemesterId}/contests/{contestId}/join  →  Join Contest (Student)
    // ──────────────────────────────────────────
    [HttpPost("{classSemesterId:guid}/contests/{contestId:guid}/join")]
    public async Task<IActionResult> JoinContest(
        Guid classSemesterId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            // Verify class membership
            var isMember = await _db.ClassMembers.AsNoTracking()
                .AnyAsync(m => m.ClassSemesterId == classSemesterId && m.UserId == userId.Value && m.IsActive, ct);
            if (!isMember) return Forbid();

            // Verify contest belongs to class instance
            var slot = await _db.ClassSlots.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
            if (slot is null) return NotFound(new { Message = "Contest not found in this class instance." });

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

    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string colName)
    {
        if (headers.TryGetValue(colName, out int colIndex))
        {
            return row.Cell(colIndex).GetValue<string>();
        }
        return "";
    }

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : null;
    }
}

