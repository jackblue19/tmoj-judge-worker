using Contracts.Submissions.Judging;

namespace Worker.Services;

public sealed class LocalJudgeService
{
    public Task<CompileExecutionResult> CompileAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        throw new NotImplementedException("Implement compile here.");
    }

    public Task<List<JudgeCaseCompletedContract>> RunCasesAsync(
        DispatchJudgeJobContract job ,
        CompileExecutionResult compile ,
        CancellationToken ct)
    {
        throw new NotImplementedException("Implement testcase execution here.");
    }
}

public sealed class CompileExecutionResult
{
    public bool Ok { get; init; }
    public int ExitCode { get; init; }
    public int? TimeMs { get; init; }
    public string? Stdout { get; init; }
    public string? Stderr { get; init; }
}