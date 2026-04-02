using Contracts.Submissions.Judging;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class JudgeWorkerHeartbeatService
{
    private readonly TmojDbContext _db;

    public JudgeWorkerHeartbeatService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> RegisterAsync(
        JudgeWorkerRegistrationContract req ,
        CancellationToken ct)
    {
        if ( string.IsNullOrWhiteSpace(req.Name) )
            throw new InvalidOperationException("Worker name is required.");

        JudgeWorker? worker = null;

        if ( req.WorkerId.HasValue && req.WorkerId.Value != Guid.Empty )
        {
            worker = await _db.JudgeWorkers
                .FirstOrDefaultAsync(x => x.Id == req.WorkerId.Value , ct);
        }

        if ( worker is null )
        {
            worker = await _db.JudgeWorkers
                .FirstOrDefaultAsync(x => x.Name == req.Name , ct);
        }

        var capabilities = req.SupportedRuntimeProfileKeys?.Distinct().ToList()
            ?? new List<string>();

        if ( worker is null )
        {
            worker = new JudgeWorker
            {
                Id = req.WorkerId is { } wid && wid != Guid.Empty ? wid : Guid.NewGuid() ,
                Name = req.Name ,
                Status = req.Status ,
                Version = req.Version ,
                LastSeenAt = DateTime.UtcNow ,
                Capabilities = capabilities
            };

            _db.JudgeWorkers.Add(worker);
        }
        else
        {
            worker.Name = req.Name;
            worker.Status = req.Status;
            worker.Version = req.Version;
            worker.LastSeenAt = DateTime.UtcNow;
            worker.Capabilities = capabilities;
        }

        await _db.SaveChangesAsync(ct);
        return worker.Id;
    }

    public async Task HeartbeatAsync(
        JudgeWorkerHeartbeatContract req ,
        CancellationToken ct)
    {
        if ( req.WorkerId == Guid.Empty )
            throw new InvalidOperationException("WorkerId is required.");

        if ( string.IsNullOrWhiteSpace(req.Name) )
            throw new InvalidOperationException("Worker name is required.");

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
                Version = req.Version ,
                Capabilities = req.Capabilities?.Distinct().ToList() ?? new List<string>()
            };

            _db.JudgeWorkers.Add(worker);
        }
        else
        {
            worker.Name = req.Name;
            worker.LastSeenAt = DateTime.UtcNow;
            worker.Status = req.Status;
            worker.Version = req.Version;
            worker.Capabilities = req.Capabilities?.Distinct().ToList() ?? new List<string>();
        }

        await _db.SaveChangesAsync(ct);
    }
}