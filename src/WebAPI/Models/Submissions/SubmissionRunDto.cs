namespace WebAPI.Models.Submissions;

public sealed class SubmissionRunDto
{
    public Guid JudgeRunId { get; set; }
    public Guid? WorkerId { get; set; }

    public string Status { get; set; } = null!;
    public string? DockerImage { get; set; }
    public string? Limits { get; set; }
    public string? Note { get; set; }

    public int? CompileExitCode { get; set; }
    public int? CompileTimeMs { get; set; }
    public int? TotalTimeMs { get; set; }
    public int? TotalMemoryKb { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}