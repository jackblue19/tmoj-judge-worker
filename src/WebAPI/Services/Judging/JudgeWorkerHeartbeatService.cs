using System.Text.Json;
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
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.Name == req.Name , ct);
        }

        var capabilitiesJson = JsonSerializer.Serialize(new
        {
            maxParallelJobs = req.MaxParallelJobs ,
            supportedRuntimeProfileKeys = req.SupportedRuntimeProfileKeys
        });

        if ( worker is null )
        {
            worker = new JudgeWorker
            {
                Id = req.WorkerId is { } wid && wid != Guid.Empty ? wid : Guid.NewGuid() ,
                Name = req.Name ,
                Status = req.Status ,
                Version = req.Version ,
                LastSeenAt = DateTime.UtcNow ,
                Capabilities = JsonDocument.Parse(capabilitiesJson).RootElement
            };

            _db.JudgeWorkers.Add(worker);
        }
        else
        {
            worker.Name = req.Name;
            worker.Status = req.Status;
            worker.Version = req.Version;
            worker.LastSeenAt = DateTime.UtcNow;
            worker.Capabilities = JsonDocument.Parse(capabilitiesJson).RootElement;
        }

        await _db.SaveChangesAsync(ct);
        return worker.Id;
    }

    public async Task HeartbeatAsync(
        JudgeWorkerHeartbeatContract req ,
        CancellationToken ct)
    {
        var worker = await _db.JudgeWorkers
            .FirstOrDefaultAsync(x => x.Id == req.WorkerId , ct)
            ?? throw new InvalidOperationException($"JudgeWorker {req.WorkerId} not found.");

        var capabilitiesJson = JsonSerializer.Serialize(new
        {
            maxParallelJobs = req.MaxParallelJobs ,
            runningJobs = req.RunningJobs ,
            cpuUsagePercent = req.CpuUsagePercent ,
            memoryUsedMb = req.MemoryUsedMb ,
            memoryTotalMb = req.MemoryTotalMb ,
            loadAverage1m = req.LoadAverage1m ,
            uptimeSeconds = req.UptimeSeconds ,
            supportedRuntimeProfileKeys = req.SupportedRuntimeProfileKeys
        });

        worker.Status = req.Status;
        worker.Version = req.Version;
        worker.LastSeenAt = DateTime.UtcNow;
        worker.Capabilities = JsonDocument.Parse(capabilitiesJson).RootElement;

        await _db.SaveChangesAsync(ct);
    }
}