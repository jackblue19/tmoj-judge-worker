namespace Worker.Execution.Runtimes.Cp;

public sealed class PythonExecutorProfile : ICpExecutorProfile
{
    public string ProfileKey => "python3-default";
    public string LanguageCode => "python";
    public string Version => "3";

    public string Name => "python3";
    public string DefaultSourceFileName => "main.py";
    public bool DefaultHasCompileStep => false;
    public string DefaultCompileCommand => "";
    public string DefaultRunCommand => "python3 main.py";
    public string? CompiledArtifactFileName => null;
}