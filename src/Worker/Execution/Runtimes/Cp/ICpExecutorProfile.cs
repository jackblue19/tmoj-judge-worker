namespace Worker.Execution.Runtimes.Cp;

public interface ICpExecutorProfile
{
    string ProfileKey { get; }
    string LanguageCode { get; }
    string Version { get; }

    string Name { get; }
    string SourceFileName { get; }
    bool HasCompileStep { get; }
    string CompileCommand { get; }
    string RunCommand { get; }

    string? CompiledArtifactFileName { get; }
}