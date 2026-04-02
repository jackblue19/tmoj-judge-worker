namespace Contracts.Submissions.Judging;

public sealed class JudgeWorkerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Status { get; init; }
    public string? Version { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public List<string> Capabilities { get; init; } = new();
}