using Contracts.Submissions.Judging;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Domain.Entities;

namespace WebAPI.Controllers.Internal;

[ApiController]
[Route("api/internal/judge/workers")]
public sealed class JudgeWorkersController : ControllerBase
{
    private readonly TmojDbContext _db;

    public JudgeWorkersController(TmojDbContext db)
    {
        _db = db;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromBody] JudgeWorkerHeartbeatContract req ,
        CancellationToken ct)
    {
        if ( req.WorkerId == Guid.Empty )
            return BadRequest(new { error = "WorkerId is required." });

        if ( string.IsNullOrWhiteSpace(req.Name) )
            return BadRequest(new { error = "Name is required." });

        var worker = await _db.JudgeWorkers
            .FirstOrDefaultAsync(x => x.Id == req.WorkerId , ct);

        if ( worker is null )
        {
            worker = new JudgeWorker
            {
                Id = req.WorkerId ,
                Name = req.Name ,
                LastSeenAt = DateTime.UtcNow ,
                Status = req.Status ,
                Version = req.Version
            };

            SetCapabilities(worker , req.Capabilities);
            _db.JudgeWorkers.Add(worker);
        }
        else
        {
            worker.Name = req.Name;
            worker.LastSeenAt = DateTime.UtcNow;
            worker.Status = req.Status;
            worker.Version = req.Version;

            SetCapabilities(worker , req.Capabilities);
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            ok = true ,
            workerId = worker.Id ,
            worker.Name ,
            worker.Status ,
            worker.LastSeenAt
        });
    }

    [HttpGet("online")]
    public async Task<IActionResult> GetOnline(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddMinutes(-2);

        var items = await _db.JudgeWorkers
            .AsNoTracking()
            .Where(x => x.LastSeenAt != null && x.LastSeenAt >= since)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id ,
                x.Name ,
                x.Status ,
                x.Version ,
                x.Capabilities ,
                x.LastSeenAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    private static void SetCapabilities(JudgeWorker worker , List<string> capabilities)
    {
        worker.Capabilities = capabilities ?? new List<string>();
    }
}