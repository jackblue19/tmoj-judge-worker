using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;

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
                SubjectId = Guid.NewGuid(),
                Code = codeNorm,
                Name = req.Name.Trim(),
                Description = req.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
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
    [Authorize(Roles = "admin,manager")]
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
}
