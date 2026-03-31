namespace Worker.Execution.Runtimes.Cp;

public sealed class JavaExecutorProfile : ICpExecutorProfile
{
    public string Name => "java";
    public string SourceFileName => "Main.java";
    public bool HasCompileStep => true;
    public string CompileCommand => "javac Main.java";
    public string RunCommand => "java Main";
}