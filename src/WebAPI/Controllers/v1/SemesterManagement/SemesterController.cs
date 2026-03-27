using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Controllers.v1.SemesterManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SemesterController : ControllerBase
{
    private readonly TmojDbContext _db;

    public SemesterController(TmojDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Semesters.AsNoTracking().Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
              .OrderByDescending(x => x.StartAt)   // sắp xếp trước
              .Skip((page - 1) * pageSize)         // bỏ qua các record trước
              .Take(pageSize)                    
              .Select(x => new SemesterResponse(
                  x.SemesterId, x.Code, x.Name, x.StartAt, x.EndAt, x.IsActive, x.CreatedAt))
              .ToListAsync(ct);

            return Ok(ApiResponse<SemesterListResponse>.Ok(
                new SemesterListResponse(items, totalCount), "Semesters fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching semesters." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var s = await _db.Semesters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SemesterId == id, ct);

            if (s is null) return NotFound(new { Message = "Semester not found." });

            return Ok(ApiResponse<SemesterResponse>.Ok(
                new SemesterResponse(s.SemesterId, s.Code, s.Name, s.StartAt, s.EndAt, s.IsActive, s.CreatedAt),
                "Semester fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the semester." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { Message = "Code and Name are required." });

            if (await _db.Semesters.AnyAsync(s => s.Code == req.Code, ct))
                return Conflict(new { Message = $"Semester code '{req.Code}' already exists." });

            var semester = new Semester
            {
                Code = req.Code.Trim().ToUpperInvariant(),
                Name = req.Name.Trim(),
                StartAt = req.StartAt,
                EndAt = req.EndAt
            };

            _db.Semesters.Add(semester);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = semester.SemesterId },
                ApiResponse<SemesterResponse>.Ok(
                    new SemesterResponse(semester.SemesterId, semester.Code, semester.Name, semester.StartAt, semester.EndAt, semester.IsActive, semester.CreatedAt),
                    "Semester created successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the semester." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSemesterRequest req, CancellationToken ct)
    {
        try
        {
            var s = await _db.Semesters.FirstOrDefaultAsync(x => x.SemesterId == id, ct);
            if (s is null) return NotFound(new { Message = "Semester not found." });

            if (await _db.Semesters.AnyAsync(x => x.Code == req.Code && x.SemesterId != id, ct))
                return Conflict(new { Message = $"Semester code '{req.Code}' already exists." });

            s.Code = req.Code.Trim().ToUpperInvariant();
            s.Name = req.Name.Trim();
            s.StartAt = req.StartAt;
            s.EndAt = req.EndAt;
            s.IsActive = req.IsActive;

            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<SemesterResponse>.Ok(
                new SemesterResponse(s.SemesterId, s.Code, s.Name, s.StartAt, s.EndAt, s.IsActive, s.CreatedAt),
                "Semester updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the semester." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var s = await _db.Semesters.FirstOrDefaultAsync(x => x.SemesterId == id, ct);
            if (s is null) return NotFound(new { Message = "Semester not found." });

            // Check if any classes are using this semester
            if (await _db.Classes.AnyAsync(c => c.SemesterId == id && c.IsActive, ct))
                return BadRequest(new { Message = "Cannot delete semester because it is being used by active classes." });

            s.IsActive = false;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Semester deleted successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the semester." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet("all-semester")]
    public async Task<IActionResult> GetAllByManager(
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
    {
        try
        {
            var query = _db.Semesters.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(s) || x.Name.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SemesterResponse(
                    x.SemesterId, x.Code, x.Name, x.StartAt, x.EndAt, x.IsActive, x.CreatedAt))
                .ToListAsync(ct);

            return Ok(ApiResponse<SemesterListResponse>.Ok(
                new SemesterListResponse(items, totalCount), "Semesters fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching semesters." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet("import/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "Code", "Name", "StartAt", "EndAt" };

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            worksheet.Cell(2, 1).Value = "FA24";
            worksheet.Cell(2, 2).Value = "Fall 2024";
            worksheet.Cell(2, 3).Value = "2024-09-01";
            worksheet.Cell(2, 4).Value = "2024-12-31";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Semester_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("import")]
    public async Task<IActionResult> ImportSemesters(IFormFile file, CancellationToken ct)
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
            var rows = worksheet.RowsUsed().Skip(1); 

            var headerRow = worksheet.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;
            }

            foreach (var row in rows)
            {
                totalProcessed++;
                try
                {
                    string codeRaw = GetCellString(row, headers, "Code");
                    string nameRaw = GetCellString(row, headers, "Name");
                    string startAtRaw = GetCellString(row, headers, "StartAt");
                    string endAtRaw = GetCellString(row, headers, "EndAt");

                    if (string.IsNullOrWhiteSpace(codeRaw) || string.IsNullOrWhiteSpace(nameRaw))
                    {
                        errors.Add($"Row {row.RowNumber()}: Missing Required fields (Code, Name).");
                        failedCount++;
                        continue;
                    }

                    var code = codeRaw.Trim().ToUpperInvariant();
                    var name = nameRaw.Trim();
                    
                    DateOnly? startAt = null;
                    DateOnly? endAt = null;

                    if (!string.IsNullOrWhiteSpace(startAtRaw) && DateOnly.TryParse(startAtRaw, out var sDate))
                        startAt = sDate;

                    if (!string.IsNullOrWhiteSpace(endAtRaw) && DateOnly.TryParse(endAtRaw, out var eDate))
                        endAt = eDate;

                    var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Code == code, ct);
                    if (semester != null)
                    {
                        semester.Name = name;
                        if (startAt.HasValue) semester.StartAt = startAt.Value;
                        if (endAt.HasValue) semester.EndAt = endAt.Value;
                    }
                    else
                    {
                        semester = new Semester
                        {
                            Code = code,
                            Name = name,
                            StartAt = startAt ?? DateOnly.FromDateTime(DateTime.UtcNow),
                            EndAt = endAt ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(4))
                        };
                        _db.Semesters.Add(semester);
                    }
                    
                    await _db.SaveChangesAsync(ct);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                    failedCount++;
                }
            }

            return Ok(ApiResponse<object>.Ok(new { TotalProcessed = totalProcessed, SuccessCount = successCount, FailedCount = failedCount, Errors = errors }, "Import processed."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet("export")]
    public async Task<IActionResult> ExportSemesters(CancellationToken ct)
    {
        try
        {
            var semesters = await _db.Semesters.AsNoTracking().OrderByDescending(s => s.StartAt).ToListAsync(ct);
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Semesters");

            var headers = new List<string> { "Code", "Name", "StartAt", "EndAt", "IsActive" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var s in semesters)
            {
                worksheet.Cell(row, 1).Value = s.Code;
                worksheet.Cell(row, 2).Value = s.Name;
                worksheet.Cell(row, 3).Value = s.StartAt.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 4).Value = s.EndAt.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 5).Value = s.IsActive ? "Yes" : "No";
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Semesters_Export.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while exporting: " + ex.Message });
        }
    }

    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string columnName)
    {
        if (headers.TryGetValue(columnName, out int colIdx))
        {
            try
            {
                var cell = row.Cell(colIdx);
                if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
                    return cell.GetDateTime().ToString("yyyy-MM-dd");
                return cell.GetString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return row.Cell(colIdx).GetValue<string>()?.Trim() ?? string.Empty;
            }
        }
        return string.Empty;
    }
}
