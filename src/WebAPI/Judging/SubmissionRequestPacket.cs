using System.Text.Json.Serialization;

namespace WebAPI.Judging;

public sealed class SubmissionRequestPacket
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "submission-request";

    [JsonPropertyName("submission-id")]
    public long SubmissionId { get; set; }

    [JsonPropertyName("problem-id")]
    public string ProblemId { get; set; } = default!;

    [JsonPropertyName("language")]
    public string Language { get; set; } = default!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;

    [JsonPropertyName("time-limit")]
    public int TimeLimitMs { get; set; }

    [JsonPropertyName("memory-limit")]
    public int MemoryLimitMb { get; set; }

    [JsonPropertyName("meta")]
    public object? Meta { get; set; }
}

public sealed class SubmissionAcknowledgedPacket
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "submission-acknowledged";

    [JsonPropertyName("submission-id")]
    public long SubmissionId { get; set; }
}

public sealed class GradingBeginPacket
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "grading-begin";

    [JsonPropertyName("submission-id")]
    public long SubmissionId { get; set; }

    [JsonPropertyName("pretested")]
    public bool Pretested { get; set; }
}

public sealed class TestCaseStatusPacket
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "test-case-status";

    [JsonPropertyName("submission-id")]
    public long SubmissionId { get; set; }

    [JsonPropertyName("case")]
    public int Case { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!; // AC/WA/TLE/...

    [JsonPropertyName("time")]
    public int TimeMs { get; set; }

    [JsonPropertyName("memory")]
    public int MemoryKb { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("batch")]
    public int Batch { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }
}
