using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers.v1.RuntimeManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class RuntimesController : ControllerBase
{
    private readonly TmojDbContext _db;

    public RuntimesController(TmojDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Runtimes
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.RuntimeName)
            .Select(x => new RuntimeDto
            {
                Id = x.Id ,
                RuntimeName = x.RuntimeName ,
                RuntimeVersion = x.RuntimeVersion ,
                ImageRef = x.ImageRef ,
                DefaultTimeLimitMs = x.DefaultTimeLimitMs ,
                DefaultMemoryLimitKb = x.DefaultMemoryLimitKb ,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id , CancellationToken ct)
    {
        var x = await _db.Runtimes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id , ct);
        if ( x is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Runtime not found.");

        return Ok(new RuntimeDto
        {
            Id = x.Id ,
            RuntimeName = x.RuntimeName ,
            RuntimeVersion = x.RuntimeVersion ,
            ImageRef = x.ImageRef ,
            DefaultTimeLimitMs = x.DefaultTimeLimitMs ,
            DefaultMemoryLimitKb = x.DefaultMemoryLimitKb ,
            IsActive = x.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRuntimeRequest req , CancellationToken ct)
    {
        if ( string.IsNullOrWhiteSpace(req.RuntimeName) )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "RuntimeName is required.");

        if ( req.DefaultTimeLimitMs <= 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "DefaultTimeLimitMs must be > 0.");

        if ( req.DefaultMemoryLimitKb <= 0 )
            return Problem(statusCode: StatusCodes.Status400BadRequest , title: "DefaultMemoryLimitKb must be > 0.");

        var entity = new Runtime
        {
            Id = Guid.NewGuid() ,
            RuntimeName = req.RuntimeName.Trim() ,
            RuntimeVersion = string.IsNullOrWhiteSpace(req.RuntimeVersion) ? null : req.RuntimeVersion.Trim() ,
            ImageRef = string.IsNullOrWhiteSpace(req.ImageRef) ? null : req.ImageRef.Trim() ,
            DefaultTimeLimitMs = req.DefaultTimeLimitMs ,
            DefaultMemoryLimitKb = req.DefaultMemoryLimitKb ,
            IsActive = req.IsActive
        };

        _db.Runtimes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById) , new { id = entity.Id } , new RuntimeDto
        {
            Id = entity.Id ,
            RuntimeName = entity.RuntimeName ,
            RuntimeVersion = entity.RuntimeVersion ,
            ImageRef = entity.ImageRef ,
            DefaultTimeLimitMs = entity.DefaultTimeLimitMs ,
            DefaultMemoryLimitKb = entity.DefaultMemoryLimitKb ,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id , [FromBody] UpdateRuntimeRequest req , CancellationToken ct)
    {
        var entity = await _db.Runtimes.FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( entity is null )
            return Problem(statusCode: StatusCodes.Status404NotFound , title: "Runtime not found.");

        if ( !string.IsNullOrWhiteSpace(req.RuntimeName) )
            entity.RuntimeName = req.RuntimeName.Trim();

        if ( req.RuntimeVersion is not null )
            entity.RuntimeVersion = string.IsNullOrWhiteSpace(req.RuntimeVersion) ? null : req.RuntimeVersion.Trim();

        if ( req.ImageRef is not null )
            entity.ImageRef = string.IsNullOrWhiteSpace(req.ImageRef) ? null : req.ImageRef.Trim();

        if ( req.DefaultTimeLimitMs.HasValue )
        {
            if ( req.DefaultTimeLimitMs.Value <= 0 )
                return Problem(statusCode: StatusCodes.Status400BadRequest , title: "DefaultTimeLimitMs must be > 0.");
            entity.DefaultTimeLimitMs = req.DefaultTimeLimitMs.Value;
        }

        if ( req.DefaultMemoryLimitKb.HasValue )
        {
            if ( req.DefaultMemoryLimitKb.Value <= 0 )
                return Problem(statusCode: StatusCodes.Status400BadRequest , title: "DefaultMemoryLimitKb must be > 0.");
            entity.DefaultMemoryLimitKb = req.DefaultMemoryLimitKb.Value;
        }

        if ( req.IsActive.HasValue )
            entity.IsActive = req.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id , CancellationToken ct)
    {
        var entity = await _db.Runtimes.FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( entity is null )
            return NoContent();

        _db.Runtimes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

