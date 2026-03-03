namespace WebAPI.Controllers.v1.SubmissionManagement;

public class SubmissionDtos
{
}

public sealed class RunSampleRequest
{
    public Guid RuntimeId { get; set; }
    public string SourceCode { get; set; } = null!;
    public int? TimeLimitMs { get; set; }
    public CompareMode? CompareMode { get; set; }
    public List<RunSampleTest> Tests { get; set; } = new();
}

public sealed class RunSampleTest
{
    public string? Input { get; set; }
    public string? ExpectedOutput { get; set; }
}

public enum CompareMode
{
    Exact = 0,
    Trim = 1,
    TrimIgnoreOutputPrefix = 2
}
