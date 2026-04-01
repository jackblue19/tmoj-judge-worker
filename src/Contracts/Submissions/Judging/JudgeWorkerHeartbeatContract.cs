namespace Contracts.Submissions.Judging;

public sealed class JudgeWorkerHeartbeatContract
{
    public Guid WorkerId { get; init; }
    public string Name { get; init; } = null!;
    public string? Version { get; init; }
    //public string? Capabilities { get; init; }
    public List<string> Capabilities { get; init; } = new();
    public string Status { get; init; } = "online";
}