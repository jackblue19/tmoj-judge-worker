namespace Worker.Execution.Runtimes.Cp;

public sealed class CppExecutorProfile : ICpExecutorProfile
{
    public string ProfileKey => "cpp17-gcc";
    public string LanguageCode => "cpp";
    public string Version => "17";

    public string Name => "cpp17";
    public string DefaultSourceFileName => "main.cpp";
    public bool DefaultHasCompileStep => true;
    public string DefaultCompileCommand => "g++ -O2 -std=c++17 main.cpp -o main";
    public string DefaultRunCommand => "./main";
    public string? CompiledArtifactFileName => "main";
}