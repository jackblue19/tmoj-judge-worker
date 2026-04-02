namespace Worker.Execution.Runtimes.Cp;

public sealed class RuntimeExecutionPlan
{
    public string ProfileKey { get; init; } = null!;
    public string SourceFileName { get; init; } = null!;
    public bool HasCompileStep { get; init; }
    public string CompileCommand { get; init; } = "";
    public string RunCommand { get; init; } = null!;
    public string? CompiledArtifactFileName { get; init; }
}