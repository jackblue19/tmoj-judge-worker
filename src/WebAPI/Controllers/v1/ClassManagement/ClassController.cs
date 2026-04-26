using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using MediatR;
using Application.UseCases.ClassSlots.Queries;
using Application.UseCases.Classes.Dtos;
using Application.UseCases.Classes.Commands.AssignTeacherRole;
using Application.UseCases.Classes.Commands.CreateClass;
using Application.UseCases.Classes.Commands.DeleteClass;
using Application.UseCases.Classes.Commands.UpdateClass;
using Application.UseCases.Classes.Commands.AddClassSemester;
using Application.UseCases.Classes.Commands.RemoveClassSemester;
using Application.UseCases.Classes.Commands.UpdateClassSemester;
using Application.UseCases.Classes.Commands.GenerateInviteCode;
using Application.UseCases.Classes.Commands.CancelInviteCode;
using Application.UseCases.Classes.Commands.JoinClassByCode;
using Application.UseCases.Classes.Commands.AddStudentManually;
using Application.UseCases.Classes.Commands.UpdateStudentStatus;
using Application.UseCases.Classes.Commands.RemoveStudent;
using Application.UseCases.Classes.Commands.CreateClassContest;
using Application.UseCases.Classes.Commands.ExtendContestTime;
using Application.UseCases.Classes.Commands.JoinContest;
using Application.UseCases.Classes.Queries.GetClasses;
using Application.UseCases.Classes.Queries.GetClassById;
using Application.UseCases.Classes.Queries.GetMyClasses;
using Application.UseCases.Classes.Queries.GetClassStudents;
using Application.UseCases.Classes.Queries.GetClassInviteCode;
using Application.UseCases.Classes.Queries.GetAvailableStudents;
using Application.UseCases.Classes.Queries.GetClassContests;
using Application.UseCases.Classes.Queries.GetClassContestById;
using Application.UseCases.Auth.Hasher;

namespace WebAPI.Controllers.v1.ClassManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClassController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMediator _mediator;

    public ClassController(TmojDbContext db, IPasswordHasher passwordHasher, IMediator mediator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
    }

    // ──────────────────────────────────────────
    // POST api/v1/class  →  Create Class (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassBody req, CancellationToken ct)
    {
        try
        {
            var (classId, instanceId) = await _mediator.Send(
                new CreateClassCommand(req.ClassCode, req.SubjectId, req.SemesterId, req.TeacherId), ct);

            return CreatedAtAction(nameof(GetById), new { id = classId },
                ApiResponse<object>.Ok(new { classId, instanceId, req.ClassCode }, "Class instance created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the class.", Detail = ex.InnerException?.Message ?? ex.Message });
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
            var result = await _mediator.Send(
                new GetClassesQuery(semesterId, subjectId, search, page, pageSize), ct);

            return Ok(ApiResponse<ClassListDto>.Ok(result, "Classes fetched successfully"));
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
            var dto = await _mediator.Send(new GetClassByIdQuery(id), ct);
            return Ok(ApiResponse<ClassDto>.Ok(dto, "Class fetched successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Class not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the class." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/my-classes/student
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

            var result = await _mediator.Send(
                new GetMyClassesQuery(userId.Value, "student", semesterId, subjectId, page, pageSize), ct);

            return Ok(ApiResponse<ClassListDto>.Ok(result, "Student classes fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching student classes." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/my-classes/teacher
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

            var result = await _mediator.Send(
                new GetMyClassesQuery(userId.Value, "teacher", semesterId, subjectId, page, pageSize), ct);

            return Ok(ApiResponse<ClassListDto>.Ok(result, "Teacher classes fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching teacher classes." });
        }
    }

    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassBody req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateClassCommand(id, req.IsActive), ct);
            return Ok(ApiResponse<object>.Ok(new { ClassId = id }, "Class updated successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Class not found." });
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
            await _mediator.Send(new DeleteClassCommand(id), ct);
            return Ok(ApiResponse<object>.Ok(new { ClassId = id }, "Class deleted (deactivated) successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Class not found." });
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
    public async Task<IActionResult> AssignTeacherRole([FromBody] AssignTeacherRoleCommand req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(req, ct);
            return Ok(new { Message = "Teacher role assigned successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while assigning teacher role." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/join  →  Submit Invite Code (Student)
    // ──────────────────────────────────────────
    [HttpPost("join")]
    public async Task<IActionResult> JoinByInviteCode([FromBody] JoinByCodeBody req, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.InviteCode))
                return BadRequest(new { Message = "Invite code is required." });

            var (classId, instanceId) = await _mediator.Send(
                new JoinClassByCodeCommand(req.InviteCode, userId.Value), ct);

            return Ok(new { Message = "Joined class instance successfully.", classId, instanceId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
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
    public async Task<IActionResult> AddSemester(Guid id, [FromBody] AddClassSemesterBody req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AddClassSemesterCommand(id, req.SemesterId, req.SubjectId, req.TeacherId), ct);
            return Ok(new { Message = "Class linked to semester successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while linking class to semester." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{id}/semesters/{classSemesterId}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}/semesters/{classSemesterId:guid}")]
    public async Task<IActionResult> RemoveSemester(Guid id, Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RemoveClassSemesterCommand(id, classSemesterId), ct);
            return Ok(new { Message = "Class-Semester instance deleted successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Class-Semester instance not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting class semester instance." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{id}/semesters/{classSemesterId}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}/semesters/{classSemesterId:guid}")]
    public async Task<IActionResult> UpdateSemester(
        Guid id, Guid classSemesterId,
        [FromBody] UpdateClassSemesterBody req,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(
                new UpdateClassSemesterCommand(id, classSemesterId, req.ClassId, req.SemesterId, req.SubjectId, req.TeacherId), ct);

            return Ok(ApiResponse<object>.Ok(
                new { ClassSemesterId = classSemesterId },
                "Class-Semester instance updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating class semester instance." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/export/template
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpGet("export/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "ClassCode", "SubjectCode", "SemesterCode", "TeacherEmail" };
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
            worksheet.Cell(2, 4).Value = "teacher@domain.com";
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Class_Import_Template.xlsx");
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
            int successCount = 0, failedCount = 0, totalProcessed = 0;
            var errors = new List<string>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RowsUsed().Skip(1);

            var headerRow = worksheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
                headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;

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
                    if (!subjectsDict.TryGetValue(subjectCodeRaw.Trim().ToLowerInvariant(), out var subjectId))
                    {
                        errors.Add($"Row {row.RowNumber()}: Subject '{subjectCodeRaw}' not found.");
                        failedCount++;
                        continue;
                    }
                    if (!semestersDict.TryGetValue(semesterCodeRaw.Trim().ToLowerInvariant(), out var semesterId))
                    {
                        errors.Add($"Row {row.RowNumber()}: Semester '{semesterCodeRaw}' not found.");
                        failedCount++;
                        continue;
                    }

                    Guid? teacherId = null;
                    if (!string.IsNullOrWhiteSpace(teacherEmailRaw))
                    {
                        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Email == teacherEmailRaw.Trim().ToLowerInvariant(), ct);
                        if (teacher == null)
                        {
                            errors.Add($"Row {row.RowNumber()}: Teacher '{teacherEmailRaw}' not found.");
                            failedCount++;
                            continue;
                        }
                        teacherId = teacher.UserId;
                    }

                    var existingClass = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == classCode, ct);
                    if (existingClass != null)
                    {
                        var alreadyLinked = await _db.ClassSemesters.AnyAsync(cs =>
                            cs.ClassId == existingClass.ClassId && cs.SemesterId == semesterId && cs.SubjectId == subjectId, ct);
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

                    var cls = new Domain.Entities.Class { ClassCode = classCode, IsActive = true };
                    _db.Classes.Add(cls);
                    await _db.SaveChangesAsync(ct);

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

            return Ok(ApiResponse<object>.Ok(
                new { TotalProcessed = totalProcessed, SuccessCount = successCount, FailedCount = failedCount, Errors = errors },
                "Import processed with results."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students/import/template
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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "Email", "RollNumber", "MemberCode", "FullName" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            worksheet.Cell(2, 1).Value = "student@domain.com";
            worksheet.Cell(2, 2).Value = "SE170001";
            worksheet.Cell(2, 3).Value = "HE170001";
            worksheet.Cell(2, 4).Value = "Nguyen Van A";
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
    // POST api/v1/class/{classSemesterId}/students/import
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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            int successCount = 0, failedCount = 0, totalProcessed = 0;
            var errors = new List<string>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RowsUsed().Skip(1);

            var headerRow = worksheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
                headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;

            var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student", ct);

            foreach (var row in rows)
            {
                totalProcessed++;
                try
                {
                    string emailRaw = GetCellString(row, headers, "Email");
                    string rollNumberRaw = GetCellString(row, headers, "RollNumber");
                    string memberCodeRaw = GetCellString(row, headers, "MemberCode");
                    string fullNameRaw = GetCellString(row, headers, "FullName");

                    var nameParts = (fullNameRaw ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                    var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                    if (string.IsNullOrWhiteSpace(emailRaw))
                    {
                        errors.Add($"Row {row.RowNumber()}: Email is required.");
                        failedCount++;
                        continue;
                    }

                    var email = emailRaw.Trim().ToLowerInvariant();
                    var memberCode = memberCodeRaw?.Trim();
                    var rollNumber = rollNumberRaw?.Trim();

                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

                    if (user == null && !string.IsNullOrWhiteSpace(memberCode))
                        user = await _db.Users.FirstOrDefaultAsync(u => u.MemberCode == memberCode, ct);

                    if (user == null && !string.IsNullOrWhiteSpace(rollNumber))
                        user = await _db.Users.FirstOrDefaultAsync(u => u.RollNumber == rollNumber, ct);

                    if (user == null)
                    {
                        var defaultPassword = !string.IsNullOrWhiteSpace(memberCodeRaw) ? memberCodeRaw.Trim()
                            : !string.IsNullOrWhiteSpace(rollNumberRaw) ? rollNumberRaw.Trim()
                            : email.Split('@')[0];

                        var baseUsername = email.Split('@')[0];
                        var username = baseUsername;
                        int suffix = 1;
                        while (await _db.Users.AnyAsync(u => u.Username == username, ct))
                            username = $"{baseUsername}{suffix++}";

                        user = new User
                        {
                            Email = email,
                            Password = _passwordHasher.Hash(defaultPassword),
                            Username = username,
                            FirstName = firstName,
                            LastName = lastName,
                            DisplayName = fullNameRaw?.Trim() ?? "",
                            RollNumber = rollNumber,
                            MemberCode = memberCode,
                            RoleId = studentRole?.RoleId,
                            EmailVerified = true,
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
                        bool updated = false;
                        if (!string.IsNullOrWhiteSpace(rollNumber) && string.IsNullOrWhiteSpace(user.RollNumber))
                        { user.RollNumber = rollNumber; updated = true; }
                        if (!string.IsNullOrWhiteSpace(memberCode) && string.IsNullOrWhiteSpace(user.MemberCode))
                        { user.MemberCode = memberCode; updated = true; }
                        if (updated) await _db.SaveChangesAsync(ct);
                    }

                    var existing = await _db.ClassMembers.FirstOrDefaultAsync(
                        m => m.ClassSemesterId == classSemesterId && m.UserId == user.UserId, ct);

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
                    errors.Add($"Row {row.RowNumber()}: {ex.InnerException?.Message ?? ex.Message}");
                    failedCount++;

                    foreach (var entry in _db.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
                        entry.State = EntityState.Detached;
                }
            }

            return Ok(ApiResponse<ImportResultDto>.Ok(
                new ImportResultDto(totalProcessed, successCount, failedCount, errors),
                "Student import processed."));
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message;
            var fullMsg = innerMsg != null
                ? $"An error occurred while importing students: {ex.Message} → {innerMsg}"
                : $"An error occurred while importing students: {ex.Message}";
            return StatusCode(500, new { Message = fullMsg });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students
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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            var members = await _mediator.Send(new GetClassStudentsQuery(classSemesterId), ct);
            return Ok(ApiResponse<List<ClassMemberDto>>.Ok(members, "Students fetched successfully."));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching students." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students/export
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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

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

            int row = 2, no = 1;
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
    // GET api/v1/class/{classSemesterId}/invite-code
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> GetInviteCode(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var dto = await _mediator.Send(new GetClassInviteCodeQuery(classSemesterId), ct);

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher")
            {
                var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
                if (instance?.TeacherId != userId.Value) return Forbid();
            }

            var msg = dto.InviteCode is null ? "No active invite code."
                    : dto.IsActive ? "Invite code is active." : "Invite code has expired.";

            return Ok(ApiResponse<InviteCodeStatusDto>.Ok(dto, msg));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching invite code." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/invite-code
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> GenerateInviteCode(Guid classSemesterId, [FromBody] GenerateInviteCodeBody req, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            var dto = await _mediator.Send(new GenerateInviteCodeCommand(classSemesterId, req.MinutesValid), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating invite code: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{classSemesterId}/invite-code
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{classSemesterId:guid}/invite-code")]
    public async Task<IActionResult> CancelInviteCode(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            await _mediator.Send(new CancelInviteCodeCommand(classSemesterId), ct);
            return Ok(new { Message = "Invite code cancelled successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while cancelling invite code." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/students/available
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("{classSemesterId:guid}/students/available")]
    public async Task<IActionResult> GetAvailableStudents(
        Guid classSemesterId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher")
            {
                var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
                if (instance is null) return NotFound(new { Message = "Class instance not found." });
                if (instance.TeacherId != userId.Value) return Forbid();
            }

            var result = await _mediator.Send(new GetAvailableStudentsQuery(classSemesterId, search, page, pageSize), ct);

            return Ok(ApiResponse<PagedAvailableStudentsDto>.Ok(result, "Available students fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching available students." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/students/manual
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/students/manual")]
    public async Task<IActionResult> AddStudentManually(Guid classSemesterId, [FromBody] AddStudentManuallyBody req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RollNumber) && string.IsNullOrWhiteSpace(req.MemberCode))
            return BadRequest(new { Message = "Must provide either RollNumber or MemberCode." });

        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            await _mediator.Send(new AddStudentManuallyCommand(classSemesterId, req.RollNumber, req.MemberCode, userId.Value), ct);
            return Ok(new { Message = "Student added successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while adding student manually: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{classSemesterId}/students/{studentId}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{classSemesterId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> UpdateStudentStatus(Guid classSemesterId, Guid studentId, [FromBody] UpdateStudentStatusBody req, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            await _mediator.Send(new UpdateStudentStatusCommand(classSemesterId, studentId, req.IsActive), ct);
            return Ok(new { Message = "Student status updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating student." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/class/{classSemesterId}/students/{studentId}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpDelete("{classSemesterId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> RemoveStudent(Guid classSemesterId, Guid studentId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var instance = await _db.ClassSemesters.AsNoTracking().FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct);
            if (instance is null) return NotFound(new { Message = "Class instance not found." });

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "teacher" && instance.TeacherId != userId.Value) return Forbid();

            await _mediator.Send(new RemoveStudentCommand(classSemesterId, studentId), ct);
            return Ok(new { Message = "Student removed from class successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while removing student." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/contests
    // ──────────────────────────────────────────
    [HttpGet("{classSemesterId:guid}/contests")]
    public async Task<IActionResult> GetAllContests(Guid classSemesterId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetClassContestsQuery(classSemesterId), ct);
            return Ok(ApiResponse<List<ClassContestSummaryDto>>.Ok(result, "Contests fetched successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Class instance not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching contests." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/contests
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{classSemesterId:guid}/contests")]
    public async Task<IActionResult> CreateContest(
        Guid classSemesterId,
        [FromBody] CreateContestBody req,
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

            var problems = req.Problems?.Select(p =>
                new ContestProblemItem(p.ProblemId, p.Ordinal, p.Alias, p.Points, p.MaxScore, p.TimeLimitMs, p.MemoryLimitKb))
                .ToList();

            var (contestId, slotId) = await _mediator.Send(
                new CreateClassContestCommand(
                    classSemesterId, userId.Value, req.Title, req.Slug, req.DescriptionMd,
                    req.StartAt, req.EndAt, req.FreezeAt, req.Rules, problems, req.SlotNo, req.SlotTitle), ct);

            return CreatedAtAction(nameof(GetContestById), new { classSemesterId, contestId, version = "1.0" },
                new { Message = "Contest created successfully.", contestId, slotId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the contest." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/class/{classSemesterId}/contests/{contestId}
    // ──────────────────────────────────────────
    [HttpGet("{classSemesterId:guid}/contests/{contestId:guid}")]
    public async Task<IActionResult> GetContestById(Guid classSemesterId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var dto = await _mediator.Send(new GetClassContestByIdQuery(classSemesterId, contestId, userId.Value), ct);
            return Ok(ApiResponse<ClassContestDto>.Ok(dto, "Contest fetched successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the contest." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/class/{classSemesterId}/contests/{contestId}/extend
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{classSemesterId:guid}/contests/{contestId:guid}/extend")]
    public async Task<IActionResult> ExtendContestTime(
        Guid classSemesterId, Guid contestId,
        [FromBody] ExtendContestBody req,
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

            await _mediator.Send(new ExtendContestTimeCommand(classSemesterId, contestId, req.NewEndAt), ct);
            return Ok(new { Message = "Contest time extended successfully." });
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
            return StatusCode(500, new { Message = "An error occurred while extending contest time." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/class/{classSemesterId}/contests/{contestId}/join
    // ──────────────────────────────────────────
    [HttpPost("{classSemesterId:guid}/contests/{contestId:guid}/join")]
    public async Task<IActionResult> JoinContest(Guid classSemesterId, Guid contestId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            await _mediator.Send(new JoinContestCommand(classSemesterId, contestId, userId.Value), ct);
            return Ok(new { Message = "Joined contest successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while joining the contest." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v{version}/class/{classId}/semester/{semesterId}/rankings
    // ──────────────────────────────────────────
    [HttpGet("{classId:guid}/semester/{semesterId:guid}/rankings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClassSemesterOverallRankings(
        Guid classId,
        Guid semesterId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetClassSemesterOverallRankingsQuery { ClassSemesterId = semesterId }, ct);

            return Ok(ApiResponse<GetClassSemesterOverallRankingsResponse>.Ok(
                result, "Fetched class semester overall rankings successfully"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Class semester not found." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching rankings." });
        }
    }

    // ── Helpers ───────────────────────────────

    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string colName)
    {
        if (headers.TryGetValue(colName, out int colIndex))
            return row.Cell(colIndex).GetValue<string>();
        return "";
    }

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : null;
    }
}

// ── Inline HTTP body records (replace WebAPI ClassRequest.cs) ──────────────
public record CreateClassBody(Guid SubjectId, Guid SemesterId, string ClassCode, Guid? TeacherId);
public record UpdateClassBody(bool? IsActive);
public record JoinByCodeBody(string InviteCode);
public record AddClassSemesterBody(Guid SemesterId, Guid SubjectId, Guid? TeacherId);
public record UpdateClassSemesterBody(Guid? ClassId, Guid? SemesterId, Guid? SubjectId, Guid? TeacherId);
public record GenerateInviteCodeBody(int MinutesValid = 15);
public record AddStudentManuallyBody(string? RollNumber, string? MemberCode);
public record UpdateStudentStatusBody(bool IsActive);
public record CreateContestBody(
    string Title, string? Slug, string? DescriptionMd,
    DateTime StartAt, DateTime EndAt, DateTime? FreezeAt,
    string? Rules,
    List<ContestProblemItem>? Problems,
    int? SlotNo, string? SlotTitle);
public record ExtendContestBody(DateTime NewEndAt);
