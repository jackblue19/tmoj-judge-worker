using Microsoft.AspNetCore.Mvc;
using System.CodeDom.Compiler;
using WebAPI.Judging;

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
    TrimIgnoreOutputPrefix = 2,
    Raw = 3
}

//  submit code
public sealed class SubmitRequest
{
    public Guid RuntimeId { get; set; }
    public string SourceCode { get; set; } = null!;
    public int? TimeLimitMs { get; set; }
    public CompareMode? CompareMode { get; set; }
    public bool? StopOnFirstFail { get; set; }

    public bool ReturnIO { get; set; } = false;
}

//public sealed class SubmitResponse
//{
//    public Guid SubmissionId { get; set; }
//    public string StatusCode { get; set; } = null!;
//    public string? VerdictCode { get; set; }
//    public CompileInfo Compile { get; set; } = new();
//    public SubmitSummary Summary { get; set; } = new();
//    public List<SubmitFailedCase> Failed { get; set; } = new();
//}

//public sealed class SubmitSummary
//{
//    public int Passed { get; set; }
//    public int Total { get; set; }
//    public int TimeMs { get; set; }
//}

//public sealed class SubmitFailedCase
//{
//    public int Ordinal { get; set; }
//    public string Verdict { get; set; } = null!;
//    public string Message { get; set; } = null!;
//}

//  riel submission

public sealed class SubmitFormDto
{
    public Guid RuntimeId { get; set; }

    public string? SourceCode { get; set; }

    [FromForm(Name = "file")]
    public IFormFile? CodeFile { get; set; }

    public int? TimeLimitMs { get; set; }
    public CompareMode? CompareMode { get; set; }
    public bool? StopOnFirstFail { get; set; }
    public bool ReturnIO { get; set; } = false;
}

public sealed class SubmitResponse
{
    public Guid SubmissionId { get; set; }
    public string StatusCode { get; set; } = null!;
    public string? VerdictCode { get; set; }

    public SubmitCompileDto Compile { get; set; } = new();

    public SubmitSummary Summary { get; set; } = new();
    public List<SubmitFailedCase> Failed { get; set; } = new();
}

public sealed class SubmitCompileDto
{
    public bool Ok { get; set; }
    public int ExitCode { get; set; }
    public string Stdout { get; set; } = "";
    public string Stderr { get; set; } = "";
}

public sealed class SubmitSummary
{
    public int Passed { get; set; }
    public int Total { get; set; }
    public int? TimeMs { get; set; }
}

public sealed class SubmitFailedCase
{
    public int Ordinal { get; set; }
    public string Verdict { get; set; } = null!;
    public string? Message { get; set; }
}
