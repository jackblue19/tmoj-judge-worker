namespace Worker.Execution.Runtimes.Cp;

public interface ICpExecutorProfile
{
    string ProfileKey { get; }
    string LanguageCode { get; }
    string Version { get; }

    string Name { get; }
    string DefaultSourceFileName { get; }
    bool DefaultHasCompileStep { get; }
    string DefaultCompileCommand { get; }
    string DefaultRunCommand { get; }

    string? CompiledArtifactFileName { get; }
}