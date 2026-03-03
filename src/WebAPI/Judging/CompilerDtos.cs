using WebAPI.Controllers.v1.SubmissionManagement;

namespace WebAPI.Judging;

public class CompilerDtos
{
}

public sealed class CompileRunRequest
{
    public Guid RuntimeId { get; set; }
    public string RuntimeName { get; set; } = null!;
    public string SourceCode { get; set; } = null!;
    public int TimeLimitMs { get; set; }
    public CompareMode CompareMode { get; set; }
    public List<SampleTest> Tests { get; set; } = new();
}

public sealed class SampleTest
{
    public int Index { get; set; }
    public string Input { get; set; } = "";
    public string ExpectedOutput { get; set; } = "";
}

public sealed class RunSampleResponse
{
    public CompileInfo Compile { get; set; } = new();
    public RunSummary Summary { get; set; } = new();
    public List<SampleCaseResult> Cases { get; set; } = new();
}

public sealed class CompileInfo
{
    public bool Ok { get; set; }
    public int ExitCode { get; set; }
    public string Stdout { get; set; } = "";
    public string Stderr { get; set; } = "";
}

public sealed class RunSummary
{
    public string Verdict { get; set; } = "ie";
    public int Passed { get; set; }
    public int Total { get; set; }
    public int TimeMs { get; set; }
}

public sealed class SampleCaseResult
{
    public int Index { get; set; }
    public string Verdict { get; set; } = "ie";
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
    public int TimeMs { get; set; }
    public string Stdout { get; set; } = "";
    public string Stderr { get; set; } = "";
    public string ExpectedOutput { get; set; } = "";
    public string ActualOutput { get; set; } = "";
}
