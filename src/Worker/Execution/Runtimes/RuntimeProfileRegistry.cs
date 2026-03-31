namespace Worker.Execution.Runtimes;

public static class RuntimeProfileRegistry
{
    public static CpRuntimeProfile Resolve(string runtimeName)
    {
        var r = runtimeName.ToLowerInvariant();

        if ( r.Contains("cpp") || r.Contains("c++") || r.Contains("prf") )
            return CpRuntimeProfile.Cpp;

        if ( r.Contains("java") || r.Contains("pro") )
            return CpRuntimeProfile.Java;

        if ( r.Contains("python") || r.Contains("pfp") )
            return CpRuntimeProfile.Python;

        throw new InvalidOperationException($"Unsupported runtime: {runtimeName}");
    }
}

public sealed class CpRuntimeProfile
{
    public string SourceFileName { get; init; } = null!;
    public string CompileCommand { get; init; } = null!;
    public string RunCommand { get; init; } = null!;
    public bool HasCompileStep { get; init; }

    public static CpRuntimeProfile Cpp => new()
    {
        SourceFileName = "main.cpp" ,
        CompileCommand = "g++ -O2 -std=c++17 main.cpp -o main" ,
        RunCommand = "./main" ,
        HasCompileStep = true
    };

    public static CpRuntimeProfile Java => new()
    {
        SourceFileName = "Main.java" ,
        CompileCommand = "javac Main.java" ,
        RunCommand = "java Main" ,
        HasCompileStep = true
    };

    public static CpRuntimeProfile Python => new()
    {
        SourceFileName = "main.py" ,
        CompileCommand = "" ,
        RunCommand = "python3 main.py" ,
        HasCompileStep = false
    };
}