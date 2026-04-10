using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Controllers.v1.SubjectManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SubjectController : ControllerBase
{
    private readonly TmojDbContext _db;

    public SubjectController(TmojDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────
    // POST api/v1/subject  →  Create Subject (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubjectRequest req,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { Message = "Code and Name are required." });

            var codeNorm = req.Code.Trim().ToUpperInvariant();

            if (await _db.Subjects.AnyAsync(s => s.Code == codeNorm, ct))
                return Conflict(new { Message = $"Subject with code '{codeNorm}' already exists." });

            var subject = new Subject
            {
                Code = codeNorm,
                Name = req.Name.Trim(),
                Description = req.Description?.Trim(),
                IsActive = true
            };

            _db.Subjects.Add(subject);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetAll), null,
                ApiResponse<SubjectResponse>.Ok(ToDto(subject), "Subject created successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the subject." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/subject  →  View All Subject (Manager)
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Subjects.AsNoTracking().Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(s)
                                      || x.Name.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SubjectResponse(
                    x.SubjectId, x.Code, x.Name,
                    x.Description, x.IsActive, x.CreatedAt))
                .ToListAsync(ct);

            return Ok(ApiResponse<SubjectListResponse>.Ok(
                new SubjectListResponse(items, totalCount),
                "Subjects fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching subjects." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/subject/{id}
    // ──────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var subject = await _db.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectId == id, ct);

            if (subject is null) return NotFound(new { Message = "Subject not found." });

            return Ok(ApiResponse<SubjectResponse>.Ok(ToDto(subject), "Subject fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the subject." });
        }
    }

    // ──────────────────────────────────────────
    // PUT api/v1/subject/{id}
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSubjectRequest req,
        CancellationToken ct)
    {
        try
        {
            var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
            if (subject is null) return NotFound(new { Message = "Subject not found." });

            if (!string.IsNullOrWhiteSpace(req.Code))
            {
                var newCode = req.Code.Trim().ToUpperInvariant();
                if (newCode != subject.Code && await _db.Subjects.AnyAsync(s => s.Code == newCode, ct))
                    return Conflict(new { Message = $"Subject code '{newCode}' already exists." });
                subject.Code = newCode;
            }
            if (!string.IsNullOrWhiteSpace(req.Name)) subject.Name = req.Name.Trim();
            if (req.Description is not null) subject.Description = req.Description.Trim();
            if (req.IsActive.HasValue) subject.IsActive = req.IsActive.Value;

            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<SubjectResponse>.Ok(ToDto(subject), "Subject updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the subject." });
        }
    }

    // helpers
    private static SubjectResponse ToDto(Subject s) =>
        new(s.SubjectId, s.Code, s.Name, s.Description, s.IsActive, s.CreatedAt);

    [Authorize(Roles = "admin,manager")]
    [HttpGet("import/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "Code", "Name", "Description" };

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            worksheet.Cell(2, 1).Value = "PRN211";
            worksheet.Cell(2, 2).Value = "C# Programming";
            worksheet.Cell(2, 3).Value = "Basic C# programming subject";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Subject_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("import")]
    public async Task<IActionResult> ImportSubjects(IFormFile file, CancellationToken ct)
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
                    string descRaw = GetCellString(row, headers, "Description");

                    if (string.IsNullOrWhiteSpace(codeRaw) || string.IsNullOrWhiteSpace(nameRaw))
                    {
                        errors.Add($"Row {row.RowNumber()}: Missing Required fields (Code, Name).");
                        failedCount++;
                        continue;
                    }

                    var code = codeRaw.Trim().ToUpperInvariant();
                    var name = nameRaw.Trim();
                    
                    var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Code == code, ct);
                    if (subject != null)
                    {
                        subject.Name = name;
                        subject.Description = descRaw;
                    }
                    else
                    {
                        subject = new Subject
                        {
                            Code = code,
                            Name = name,
                            Description = descRaw,
                            IsActive = true
                        };
                        _db.Subjects.Add(subject);
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
    public async Task<IActionResult> ExportSubjects(CancellationToken ct)
    {
        try
        {
            var subjects = await _db.Subjects.AsNoTracking().OrderBy(s => s.Code).ToListAsync(ct);
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Subjects");

            var headers = new List<string> { "Code", "Name", "Description", "IsActive" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var s in subjects)
            {
                worksheet.Cell(row, 1).Value = s.Code;
                worksheet.Cell(row, 2).Value = s.Name;
                worksheet.Cell(row, 3).Value = s.Description;
                worksheet.Cell(row, 4).Value = s.IsActive ? "Yes" : "No";
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Subjects_Export.xlsx");
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
            return row.Cell(colIdx).GetValue<string>()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }
}
