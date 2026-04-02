namespace Contracts.Submissions.Judging;

public sealed class JudgeWorkerHeartbeatContract
{
    public Guid WorkerId { get; init; }
    public string Status { get; init; } = "idle";
    public string Version { get; init; } = "1.0.0";

    public int RunningJobs { get; init; }
    public int MaxParallelJobs { get; init; }

    public double? CpuUsagePercent { get; init; }
    public long? MemoryUsedMb { get; init; }
    public long? MemoryTotalMb { get; init; }
    public double? LoadAverage1m { get; init; }

    public long UptimeSeconds { get; init; }

    public List<string> SupportedRuntimeProfileKeys { get; init; } = new();
}