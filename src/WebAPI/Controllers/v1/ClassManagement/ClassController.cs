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
    // DELETE api/v1/class/{id}/members/me  →  Leave Class (Student)
    // ──────────────────────────────────────────
    [HttpDelete("{id:guid}/members/me")]
    public async Task<IActionResult> LeaveClass(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var members = await _db.ClassMembers
                .Where(m => m.ClassSemester.ClassId == id && m.UserId == userId.Value && m.IsActive)
                .ToListAsync(ct);

            if (!members.Any()) return NotFound(new { Message = "You are not an active member in any semester of this class." });

            foreach (var m in members) m.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Left all class instances successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while leaving the class." });
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
