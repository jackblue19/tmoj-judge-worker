using Application.UseCases.Auth;
using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Controllers.v1.ProblemManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProblemsController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProblemsController(TmojDbContext db , ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET api/problems
    [HttpGet]
    public async Task<ActionResult<List<ProblemResponseDto>>> GetAll(
        [FromQuery] string? difficulty ,
        [FromQuery] string? status ,
        CancellationToken ct)
    {
        var query = _db.Problems.AsNoTracking().Where(x => x.IsActive);

        if ( !string.IsNullOrWhiteSpace(difficulty) )
            query = query.Where(x => x.Difficulty == difficulty);

        if ( !string.IsNullOrWhiteSpace(status) )
            query = query.Where(x => x.StatusCode == status);

        var result = await query
            .OrderBy(x => x.DisplayIndex)
            .Select(x => new ProblemResponseDto
            {
                Id = x.Id ,
                Slug = x.Slug ,
                Title = x.Title ,
                Difficulty = x.Difficulty ,
                StatusCode = x.StatusCode ,
                IsActive = x.IsActive ,
                AcceptancePercent = x.AcceptancePercent ,
                TimeLimitMs = x.TimeLimitMs ,
                MemoryLimitKb = x.MemoryLimitKb ,
                CreatedAt = x.CreatedAt ,
                PublishedAt = x.PublishedAt
            })
            .ToListAsync(ct);

        return Ok(result);
    }

    // GET api/problems/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProblemResponseDto>> GetById(Guid id , CancellationToken ct)
    {
        var problem = await _db.Problems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id , ct);

        if ( problem is null )
            return NotFound();

        return Ok(new ProblemResponseDto
        {
            Id = problem.Id ,
            Slug = problem.Slug ,
            Title = problem.Title ,
            Difficulty = problem.Difficulty ,
            StatusCode = problem.StatusCode ,
            IsActive = problem.IsActive ,
            Content = problem.DescriptionMd ,
            AcceptancePercent = problem.AcceptancePercent ,
            TimeLimitMs = problem.TimeLimitMs ,
            MemoryLimitKb = problem.MemoryLimitKb ,
            CreatedAt = problem.CreatedAt ,
            PublishedAt = problem.PublishedAt
        });
    }

    private Guid? GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr , out var id) ? id : null;
    }

    // POST api/v1/problems/drafts
    [HttpPost("drafts")]
    [Authorize]
    public async Task<ActionResult<ProblemResponseDto>> Create(
        [FromBody] ProblemCreateDto dto ,
        CancellationToken ct)
    {
        var existing = await _db.Problems
            .FirstOrDefaultAsync(x => x.Slug == dto.Slug , ct);

        //  fetch userId ver1
        //if ( Guid.TryParse(_currentUser.UserId , out var userId) )
        //{
        //    dto.CreatedBy = userId;
        //}
        //else
        //{
        //    throw new Exception("Invalid UserId format");
        //}

        //  fetch userId ver2
        dto.CreatedBy = _currentUser.GetUserIdAsGuid();
        //dto.CreatedBy = GetUserId();
        if ( dto.CreatedBy == null ) Console.WriteLine("del co userid");
        if ( string.IsNullOrEmpty(dto.CreatedBy.ToString()) ) Console.WriteLine("notfound404040404004");
        Console.WriteLine(dto.CreatedBy);

        //  fetch userId ver3   (có Authorize -> ko cần ver 3)
        //if ( _currentUser.TryGetUserIdAsGuid(out var userId) )
        //{
        //    dto.CreatedBy = (Guid?) userId;
        //}
        //else
        //{
        //    return Unauthorized();
        //}

        if ( existing != null )
        {
            if ( existing.IsActive )
                return Conflict("Slug already exists.");

            // Restore + overwrite
            existing.Title = dto.Title.Trim();
            existing.Difficulty = dto.Difficulty;
            existing.TypeCode = dto.TypeCode;
            existing.VisibilityCode = dto.VisibilityCode;
            existing.ScoringCode = dto.ScoringCode;
            existing.DescriptionMd = dto.DescriptionMd;
            existing.AcceptancePercent = dto.AcceptancePercent;
            existing.DisplayIndex = dto.DisplayIndex;
            existing.TimeLimitMs = dto.TimeLimitMs;
            existing.MemoryLimitKb = dto.MemoryLimitKb;
            existing.CreatedBy = dto.CreatedBy;
            existing.IsActive = true;
            existing.StatusCode = "draft";
            existing.PublishedAt = null;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return Ok(ToDto(existing));
        }

        var problem = new Problem
        {
            Id = Guid.NewGuid() ,
            Slug = dto.Slug ,
            Title = dto.Title.Trim() ,
            Difficulty = dto.Difficulty ,
            TypeCode = dto.TypeCode ,
            VisibilityCode = dto.VisibilityCode ,
            ScoringCode = dto.ScoringCode ,
            DescriptionMd = dto.DescriptionMd ,
            AcceptancePercent = dto.AcceptancePercent ,
            DisplayIndex = dto.DisplayIndex ,
            TimeLimitMs = dto.TimeLimitMs ,
            MemoryLimitKb = dto.MemoryLimitKb ,
            CreatedBy = dto.CreatedBy,
            StatusCode = "draft" ,
            CreatedAt = DateTime.UtcNow ,
            IsActive = true
        };

        _db.Problems.Add(problem);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById) ,
            new { id = problem.Id } ,
            ToDto(problem)
        );
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("drafts/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProblemResponseDto>> CreateWithMarkdownUpload(
    [FromForm] ProblemCreateFormDto dto ,
    CancellationToken ct)
    {
        if ( string.IsNullOrWhiteSpace(dto.Slug) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "Slug is required.");

        if ( string.IsNullOrWhiteSpace(dto.Title) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "Title is required.");

        string? descriptionMd = dto.DescriptionMd;

        if ( dto.DescriptionFile is not null && dto.DescriptionFile.Length > 0 )
        {
            var ext = Path.GetExtension(dto.DescriptionFile.FileName).ToLowerInvariant();
            if ( ext != ".md" )
                return Problem(statusCode: StatusCodes.Status400BadRequest , title: "Only .md is supported for descriptionFile.");

            await using var fs = dto.DescriptionFile.OpenReadStream();
            using var sr = new StreamReader(fs , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
            descriptionMd = await sr.ReadToEndAsync(ct);
        }

        var existing = await _db.Problems.FirstOrDefaultAsync(x => x.Slug == dto.Slug , ct);

        if ( existing != null )
        {
            if ( existing.IsActive )
                return Conflict("Slug already exists.");

            existing.Title = dto.Title.Trim();
            existing.Difficulty = dto.Difficulty;
            existing.TypeCode = dto.TypeCode;
            existing.VisibilityCode = dto.VisibilityCode;
            existing.ScoringCode = dto.ScoringCode;
            existing.DescriptionMd = descriptionMd;
            existing.AcceptancePercent = dto.AcceptancePercent;
            existing.DisplayIndex = dto.DisplayIndex;
            existing.TimeLimitMs = dto.TimeLimitMs;
            existing.MemoryLimitKb = dto.MemoryLimitKb;

            existing.IsActive = true;
            existing.StatusCode = "draft";
            existing.PublishedAt = null;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Ok(ToDto(existing));
        }

        var problem = new Problem
        {
            Id = Guid.NewGuid() ,
            Slug = dto.Slug ,
            Title = dto.Title.Trim() ,
            Difficulty = dto.Difficulty ,
            TypeCode = dto.TypeCode ,
            VisibilityCode = dto.VisibilityCode ,
            ScoringCode = dto.ScoringCode ,
            DescriptionMd = descriptionMd ,
            AcceptancePercent = dto.AcceptancePercent ,
            DisplayIndex = dto.DisplayIndex ,
            TimeLimitMs = dto.TimeLimitMs ,
            MemoryLimitKb = dto.MemoryLimitKb ,
            StatusCode = "draft" ,
            CreatedAt = DateTime.UtcNow ,
            IsActive = true
        };

        _db.Problems.Add(problem);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById) , new { id = problem.Id } , ToDto(problem));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    // PUT api/problems
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] ProblemUpdateDto dto , CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == dto.Id , ct);

        if ( problem is null )
            return NotFound();

        if ( !string.IsNullOrWhiteSpace(dto.Title) )
            problem.Title = dto.Title.Trim();

        if ( !string.IsNullOrWhiteSpace(dto.Slug) )
            problem.Slug = dto.Slug;

        problem.Difficulty = dto.Difficulty ?? problem.Difficulty;
        problem.TypeCode = dto.TypeCode ?? problem.TypeCode;
        problem.VisibilityCode = dto.VisibilityCode ?? problem.VisibilityCode;
        problem.ScoringCode = dto.ScoringCode ?? problem.ScoringCode;
        problem.DescriptionMd = dto.DescriptionMd ?? problem.DescriptionMd;
        problem.AcceptancePercent = dto.AcceptancePercent ?? problem.AcceptancePercent;
        problem.DisplayIndex = dto.DisplayIndex ?? problem.DisplayIndex;
        problem.TimeLimitMs = dto.TimeLimitMs ?? problem.TimeLimitMs;
        problem.MemoryLimitKb = dto.MemoryLimitKb ?? problem.MemoryLimitKb;

        if ( !string.IsNullOrWhiteSpace(dto.StatusCode) )
        {
            if ( dto.StatusCode is not ("draft" or "pending" or "published" or "archived") )
                return BadRequest("Invalid status_code");

            problem.StatusCode = dto.StatusCode;

            if ( dto.StatusCode == "published" )
                problem.PublishedAt = DateTime.UtcNow;
            else
                problem.PublishedAt = null;
        }

        problem.UpdatedAt = DateTime.UtcNow;
        problem.UpdatedBy = dto.UpdatedBy;

        await _db.SaveChangesAsync(ct);

        return Ok(problem.Id);
    }


    // DELETE api/problems/{id}     (del-ver1)      -> được thì cái del-ver2 thành soft-del còn del-ver1 này thì cho thành hard-del
    [HttpDelete("{id:guid}/hard")]
    public async Task<IActionResult> HardDelete(Guid id , CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        _db.Problems.Remove(problem);

        try
        {
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch ( DbUpdateException )
        {
            // thường là FK violation 23503
            return Conflict("Cannot hard delete: problem is referenced by other records (FK). Use archive or delete dependencies first.");
        }
    }

    // POST api/v{version}/problems/{id}/archive    (del-ver 2)
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(
    Guid id ,
    [FromBody] ProblemArchiveDto? dto ,
    CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        problem.IsActive = false;
        problem.StatusCode = "archived";

        // FIX constraint publish consistency:
        problem.PublishedAt = null;

        problem.UpdatedAt = DateTime.UtcNow;
        problem.UpdatedBy = dto?.ArchivedBy;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id , CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        problem.IsActive = false;
        problem.StatusCode = "archived";

        // FIX constraint publish consistency:
        problem.PublishedAt = null;

        problem.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    //  helpers
    private static ProblemResponseDto ToDto(Problem p)
    {
        return new ProblemResponseDto
        {
            Id = p.Id ,
            Slug = p.Slug ,
            Title = p.Title ,
            Difficulty = p.Difficulty ,
            StatusCode = p.StatusCode ,
            IsActive = p.IsActive ,
            AcceptancePercent = p.AcceptancePercent ,
            TimeLimitMs = p.TimeLimitMs ,
            MemoryLimitKb = p.MemoryLimitKb ,
            CreatedAt = p.CreatedAt ,
            PublishedAt = p.PublishedAt
        };
    }

    // PUT api/v{version}/problems/{id}/difficulty
    [HttpPut("{id:guid}/difficulty")]
    public async Task<ActionResult<ProblemResponseDto>> SetDifficulty(
        Guid id ,
        [FromBody] ProblemSetDifficultyDto dto ,
        CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == id && x.IsActive , ct);
        if ( problem is null ) return NotFound();

        problem.Difficulty = dto.Difficulty;
        problem.UpdatedAt = DateTime.UtcNow;
        problem.UpdatedBy = dto.UpdatedBy;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(problem));
    }

    // POST api/v{version}/problems/{id}/publish
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ProblemResponseDto>> Publish(
        Guid id ,
        [FromBody] ProblemPublishDto dto ,
        CancellationToken ct)
    {
        var problem = await _db.Problems.FirstOrDefaultAsync(x => x.Id == id && x.IsActive , ct);
        if ( problem is null ) return NotFound();

        if ( problem.StatusCode is "archived" )
            return Conflict("Archived problem cannot be published.");

        problem.StatusCode = "published";
        problem.PublishedAt = DateTime.UtcNow;
        problem.UpdatedAt = DateTime.UtcNow;
        problem.UpdatedBy = dto?.PublishedBy;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(problem));
    }
}