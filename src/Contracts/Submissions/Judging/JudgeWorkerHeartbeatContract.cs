namespace Contracts.Submissions.Judging;

public sealed class JudgeWorkerHeartbeatContract
{
    public Guid WorkerId { get; init; }

    public string Name { get; init; } = null!;
    public string? Version { get; init; }

    public List<string> Capabilities { get; init; } = new();

    public string Status { get; init; } = "online";

    public int RunningJobs { get; init; }
    public int MaxParallelJobs { get; init; }

    public double? CpuUsagePercent { get; init; }
    public long? MemoryUsedMb { get; init; }
    public long? MemoryTotalMb { get; init; }
    public double? LoadAverage1m { get; init; }

    public long UptimeSeconds { get; init; }
}