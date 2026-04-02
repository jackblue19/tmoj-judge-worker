namespace Contracts.Submissions.Judging;

public sealed class JudgeMetricsOverviewDto
{
    public int QueuedJobs { get; init; }
    public int RunningJobs { get; init; }
    public int DoneJobs { get; init; }
    public int FailedJobs { get; init; }

    public int ActiveWorkers { get; init; }
    public int OnlineWorkers { get; init; }

    public DateTime GeneratedAtUtc { get; init; }

    public List<JudgeWorkerDto> Workers { get; init; } = new();
}