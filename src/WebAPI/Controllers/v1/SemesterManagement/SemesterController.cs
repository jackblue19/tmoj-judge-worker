using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;

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
}
