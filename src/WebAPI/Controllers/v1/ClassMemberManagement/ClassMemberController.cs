using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using System.IO;

namespace WebAPI.Controllers.v1.ClassMemberManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClassMemberController : ControllerBase
{
    private readonly TmojDbContext _db;

    public ClassMemberController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // POST api/v1/ClassMember  →  Create (Manager/Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClassMemberRequest req,
        CancellationToken ct)
    {
        try
        {
            if (req.ClassId == null)
                return BadRequest(new { Message = "ClassId is required." });

            // Check if class exists
            if (!await _db.Classes.AnyAsync(c => c.ClassId == req.ClassId, ct))
                return BadRequest(new { Message = "Class not found." });

            // Find student
            User? student = null;
            if (req.UserId.HasValue)
                student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId.Value, ct);
            else if (!string.IsNullOrWhiteSpace(req.Email))
                student = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

            if (student is null)
                return NotFound(new { Message = "Student not found." });

            // Check if already a member
            if (await _db.ClassMembers.AnyAsync(m => m.ClassId == req.ClassId && m.UserId == student.UserId, ct))
                return Conflict(new { Message = "User is already a member of this class." });

            var member = new ClassMember
            {
                ClassId = req.ClassId.Value,
                UserId = student.UserId,
                IsActive = req.IsActive,
                JoinedAt = DateTime.UtcNow
            };

            _db.ClassMembers.Add(member);
            await _db.SaveChangesAsync(ct);

            // Fetch with user info for response
            var result = await _db.ClassMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == member.Id, ct);

            return CreatedAtAction(nameof(GetById), new { id = member.Id },
                ApiResponse<ClassMemberResponse>.Ok(ToDto(result!), "Class member added successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while adding class member." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/ClassMember  →  Get All (Manager/Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? userId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(m => m.ClassId == classId.Value);
            
            if (userId.HasValue)
                query = query.Where(m => m.UserId == userId.Value);

            if (isActive.HasValue)
                query = query.Where(m => m.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(m => m.JoinedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => ToDto(m))
                .ToListAsync(ct);

            return Ok(ApiResponse<ClassMemberListResponse>.Ok(
                new ClassMemberListResponse(items, totalCount), "Class members fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching class members." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/ClassMember/{id}
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var member = await _db.ClassMembers.AsNoTracking()
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (member is null)
                return NotFound(new { Message = "Class member not found." });

            return Ok(ApiResponse<ClassMemberResponse>.Ok(ToDto(member), "Class member fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching class member." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/ClassMember/{id}  →  Update (Manager/Teacher)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateClassMemberRequest req,
        CancellationToken ct)
    {
        try
        {
            var member = await _db.ClassMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (member is null)
                return NotFound(new { Message = "Class member not found." });

            member.IsActive = req.IsActive;
            
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<ClassMemberResponse>.Ok(ToDto(member), "Class member updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating class member." });
        }
    }

    // ──────────────────────────────────────────
    // DELETE api/v1/ClassMember/{id}  →  Delete (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var member = await _db.ClassMembers.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
                return NotFound(new { Message = "Class member not found." });

            _db.ClassMembers.Remove(member);
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new { Id = id }, "Class member removed successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while removing class member." });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/ClassMember/import/class/{classId}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("import/class/{classId:guid}")]
    public async Task<IActionResult> ImportClassSpecific(Guid classId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is missing." });

        try
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);
            if (cls == null)
                return NotFound(new { Message = "Class not found." });

            var result = await ProcessExcelImport(file, classId, ct);
            return Ok(ApiResponse<ImportResultResponse>.Ok(result, "Import processed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST api/v1/ClassMember/import/global
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost("import/global")]
    public async Task<IActionResult> ImportGlobal(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is missing." });

        try
        {
            var result = await ProcessExcelImport(file, null, ct);
            return Ok(ApiResponse<ImportResultResponse>.Ok(result, "Import processed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/ClassMember/import/template
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("import/template")]
    public IActionResult DownloadTemplate([FromQuery] bool isGlobal = false)
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            // Define headers
            var headers = new List<string>();
            if (isGlobal)
            {
                headers.Add("SubjectCode");
                headers.Add("ClassCode");
            }
            headers.Add("FullName");
            headers.Add("Email");
            headers.Add("RollNumber");
            headers.Add("MemberCode");

            // Write headers
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            // Write sample row
            int colIndex = 1;
            if (isGlobal)
            {
                worksheet.Cell(2, colIndex++).Value = "PRF192";
                worksheet.Cell(2, colIndex++).Value = "SE21A01";
            }
            worksheet.Cell(2, colIndex++).Value = "Nguyen Van A";
            worksheet.Cell(2, colIndex++).Value = "nguyenva@domain.com";
            worksheet.Cell(2, colIndex++).Value = "HE111111";
            worksheet.Cell(2, colIndex++).Value = "MEMBER001";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = isGlobal ? "Global_Import_Template.xlsx" : "Class_Import_Template.xlsx";
            
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    private async Task<ImportResultResponse> ProcessExcelImport(IFormFile file, Guid? fallbackClassId, CancellationToken ct)
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

        // Cache for global import
        var classCache = new Dictionary<string, Guid>(); // keys like "SUBJECTCODE_CLASSCODE"

        foreach (var row in rows)
        {
            totalProcessed++;
            try
            {
                // Parse required columns
                string email = GetCellString(row, headers, "Email");
                string fullName = GetCellString(row, headers, "FullName");
                
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add($"Row {row.RowNumber()}: Missing Email or FullName.");
                    failedCount++;
                    continue;
                }

                string rollNumber = GetCellString(row, headers, "RollNumber");
                string memberCode = GetCellString(row, headers, "MemberCode");

                Guid targetClassId;

                if (fallbackClassId.HasValue)
                {
                    targetClassId = fallbackClassId.Value;
                }
                else
                {
                    string subjectCode = GetCellString(row, headers, "SubjectCode");
                    string classCode = GetCellString(row, headers, "ClassCode");

                    if (string.IsNullOrWhiteSpace(subjectCode) || string.IsNullOrWhiteSpace(classCode))
                    {
                        errors.Add($"Row {row.RowNumber()}: Missing SubjectCode or ClassCode for global import.");
                        failedCount++;
                        continue;
                    }

                    string cacheKey = $"{subjectCode}_{classCode}".ToUpperInvariant();
                    if (!classCache.TryGetValue(cacheKey, out targetClassId))
                    {
                        var sc = subjectCode.ToUpperInvariant();
                        var cc = classCode.ToUpperInvariant();
                        var cls = await _db.Classes
                            .Include(c => c.Subject)
                            .FirstOrDefaultAsync(c => c.Subject.Code == sc && c.ClassCode == cc, ct);

                        if (cls == null)
                        {
                            errors.Add($"Row {row.RowNumber()}: Class {classCode} in Subject {subjectCode} not found.");
                            failedCount++;
                            continue;
                        }

                        targetClassId = cls.ClassId;
                        classCache[cacheKey] = targetClassId;
                    }
                }

                // Process User
                email = email.Trim().ToLowerInvariant();
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

                if (user == null)
                {
                    // Create new user
                    var names = SplitFullName(fullName);
                    user = new User
                    {
                        FirstName = names.FirstName,
                        LastName = names.LastName,
                        Email = email,
                        Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                        RollNumber = rollNumber,
                        MemberCode = memberCode,
                        DisplayName = fullName,
                        Status = true,
                        EmailVerified = true,
                        LanguagePreference = "en",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // Update existing if new fields are provided
                    bool updated = false;
                    if (!string.IsNullOrWhiteSpace(rollNumber) && user.RollNumber != rollNumber)
                    {
                        user.RollNumber = rollNumber;
                        updated = true;
                    }
                    if (!string.IsNullOrWhiteSpace(memberCode) && user.MemberCode != memberCode)
                    {
                        user.MemberCode = memberCode;
                        updated = true;
                    }
                    if (updated)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                    }
                }

                // Add to class if not already member
                bool exists = await _db.ClassMembers.AnyAsync(m => m.ClassId == targetClassId && m.UserId == user.UserId, ct);
                if (!exists)
                {
                    var cm = new ClassMember
                    {
                        ClassId = targetClassId,
                        UserId = user.UserId,
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow
                    };
                    _db.ClassMembers.Add(cm);
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

        return new ImportResultResponse(totalProcessed, successCount, failedCount, errors);
    }

    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string columnName)
    {
        if (headers.TryGetValue(columnName, out int colIdx))
        {
            return row.Cell(colIdx).GetValue<string>()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    private (string LastName, string FirstName) SplitFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("Unknown", "Unknown");

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], parts[0]); // LastName = First name, FirstName = Last name
        }

        string lastName = parts[0];
        string firstName = string.Join(" ", parts.Skip(1));
        return (lastName, firstName);
    }

    // ── Helpers ───────────────────────────────

    private static ClassMemberResponse ToDto(ClassMember m) =>
        new(
            m.Id,
            m.ClassId,
            m.UserId,
            m.User?.DisplayName,
            m.User?.Email,
            m.User?.AvatarUrl,
            m.JoinedAt,
            m.IsActive);
}
