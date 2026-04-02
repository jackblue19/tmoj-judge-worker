using Contracts.Submissions.Judging;
using Worker.Execution.Runtimes.Cp;

namespace Worker.Execution.Runtimes;

public sealed class RuntimeProfileRegistry
{
    private readonly Dictionary<string , ICpExecutorProfile> _profiles;

    public RuntimeProfileRegistry(
        CppExecutorProfile cpp ,
        JavaExecutorProfile java ,
        PythonExecutorProfile python)
    {
        _profiles = new Dictionary<string , ICpExecutorProfile>(StringComparer.OrdinalIgnoreCase)
        {
            [cpp.ProfileKey] = cpp ,
            [java.ProfileKey] = java ,
            [python.ProfileKey] = python
        };
    }

    public ICpExecutorProfile ResolveProfile(string runtimeProfileKey)
    {
        if ( string.IsNullOrWhiteSpace(runtimeProfileKey) )
            throw new InvalidOperationException("Runtime profile key is required.");

        if ( _profiles.TryGetValue(runtimeProfileKey.Trim() , out var profile) )
            return profile;

        throw new InvalidOperationException($"Unsupported runtime profile key: {runtimeProfileKey}");
    }

    public RuntimeExecutionPlan ResolveExecutionPlan(DispatchJudgeJobContract job)
    {
        var profile = ResolveProfile(job.RuntimeProfileKey);

        var sourceFileName = string.IsNullOrWhiteSpace(job.SourceFileName)
            ? profile.DefaultSourceFileName
            : job.SourceFileName.Trim();

        var hasCompileStep = job.HasCompileStep;

        var compileCommand = string.IsNullOrWhiteSpace(job.CompileCommand)
            ? profile.DefaultCompileCommand
            : job.CompileCommand.Trim();

        var runCommand = string.IsNullOrWhiteSpace(job.RunCommand)
            ? profile.DefaultRunCommand
            : job.RunCommand.Trim();

        return new RuntimeExecutionPlan
        {
            ProfileKey = profile.ProfileKey ,
            SourceFileName = sourceFileName ,
            HasCompileStep = hasCompileStep ,
            CompileCommand = compileCommand ,
            RunCommand = runCommand ,
            CompiledArtifactFileName = profile.CompiledArtifactFileName
        };
    }
}