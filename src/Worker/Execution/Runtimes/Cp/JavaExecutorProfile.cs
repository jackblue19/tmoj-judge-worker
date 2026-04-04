namespace Worker.Execution.Runtimes.Cp;

public sealed class JavaExecutorProfile : ICpExecutorProfile
{
    public string ProfileKey => "java-default";
    public string LanguageCode => "java";
    public string Version => "default";

    public string Name => "java";
    public string DefaultSourceFileName => "Main.java";
    public bool DefaultHasCompileStep => true;
    public string DefaultCompileCommand => "javac Main.java";
    public string DefaultRunCommand => "java Main";
    public string? CompiledArtifactFileName => "Main.class";
}