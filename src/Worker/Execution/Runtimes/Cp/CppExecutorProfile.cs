namespace Worker.Execution.Runtimes.Cp;

public sealed class CppExecutorProfile : ICpExecutorProfile
{
    public string Name => "cpp17";
    public string SourceFileName => "main.cpp";
    public bool HasCompileStep => true;
    public string CompileCommand => "g++ -O2 -std=c++17 main.cpp -o main";
    public string RunCommand => "./main";
}