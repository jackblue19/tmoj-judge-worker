namespace Contracts.Submissions.Judging;

public sealed class JudgeWorkerRegistrationContract
{
    public Guid? WorkerId { get; init; }
    public string Name { get; init; } = null!;
    public string Status { get; init; } = "starting";
    public string Version { get; init; } = "1.0.0";

    public int MaxParallelJobs { get; init; }
    public List<string> SupportedRuntimeProfileKeys { get; init; } = new();
}