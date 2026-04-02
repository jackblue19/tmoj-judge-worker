using Contracts.Submissions.Judging;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class JudgeMetricsService
{
    private readonly TmojDbContext _db;

    public JudgeMetricsService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JudgeWorkerDto>> GetWorkersAsync(CancellationToken ct)
    {
        var workers = await _db.JudgeWorkers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new JudgeWorkerDto
            {
                Id = x.Id ,
                Name = x.Name ,
                Status = x.Status ,
                Version = x.Version ,
                LastSeenAt = x.LastSeenAt ,
                Capabilities = x.Capabilities ?? new List<string>()
            })
            .ToListAsync(ct);

        return workers;
    }

    public async Task<JudgeWorkerDto?> GetWorkerByIdAsync(Guid workerId , CancellationToken ct)
    {
        return await _db.JudgeWorkers
            .AsNoTracking()
            .Where(x => x.Id == workerId)
            .Select(x => new JudgeWorkerDto
            {
                Id = x.Id ,
                Name = x.Name ,
                Status = x.Status ,
                Version = x.Version ,
                LastSeenAt = x.LastSeenAt ,
                Capabilities = x.Capabilities ?? new List<string>()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<JudgeMetricsOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var onlineThreshold = now.AddMinutes(-2);

        var queuedJobs = await _db.JudgeJobs.AsNoTracking().CountAsync(x => x.Status == "queued" , ct);
        var runningJobs = await _db.JudgeJobs.AsNoTracking().CountAsync(x => x.Status == "running" , ct);
        var doneJobs = await _db.JudgeJobs.AsNoTracking().CountAsync(x => x.Status == "done" , ct);
        var failedJobs = await _db.JudgeJobs.AsNoTracking().CountAsync(x => x.Status == "failed" , ct);

        var workers = await GetWorkersAsync(ct);

        return new JudgeMetricsOverviewDto
        {
            QueuedJobs = queuedJobs ,
            RunningJobs = runningJobs ,
            DoneJobs = doneJobs ,
            FailedJobs = failedJobs ,
            ActiveWorkers = workers.Count ,
            OnlineWorkers = workers.Count(x => x.LastSeenAt.HasValue && x.LastSeenAt.Value >= onlineThreshold) ,
            GeneratedAtUtc = now ,
            Workers = workers.ToList()
        };
    }
}