using Contracts.Submissions.Judging;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var worker = await _db.JudgeWorkers.FirstOrDefaultAsync(x => x.Id == req.WorkerId , ct);

        if ( worker is null )
        {
            worker = new Domain.Entities.JudgeWorker
            {
                Id = req.WorkerId ,
                Name = req.Name ,
                Capabilities = req.Capabilities ,
                LastSeenAt = DateTime.UtcNow ,
                Status = req.Status ,
                Version = req.Version
            };

            _db.JudgeWorkers.Add(worker);
        }
        else
        {
            worker.Name = req.Name;
            worker.Capabilities = req.Capabilities;
            worker.LastSeenAt = DateTime.UtcNow;
            worker.Status = req.Status;
            worker.Version = req.Version;
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
}