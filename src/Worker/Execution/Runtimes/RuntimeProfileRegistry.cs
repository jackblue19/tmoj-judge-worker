using Worker.Execution.Runtimes.Cp;

namespace Worker.Execution.Runtimes;

public sealed class RuntimeProfileRegistry
{
    private readonly CppExecutorProfile _cpp;
    private readonly JavaExecutorProfile _java;
    private readonly PythonExecutorProfile _python;

    public RuntimeProfileRegistry(
        CppExecutorProfile cpp ,
        JavaExecutorProfile java ,
        PythonExecutorProfile python)
    {
        _cpp = cpp;
        _java = java;
        _python = python;
    }

    public ICpExecutorProfile Resolve(string runtimeName)
    {
        var r = runtimeName.Trim().ToLowerInvariant();

        if ( r.Contains("cpp") || r.Contains("c++") || r.Contains("prf") )
            return _cpp;

        if ( r.Contains("java") || r.Contains("pro") )
            return _java;

        if ( r.Contains("python") || r.Contains("pfp") )
            return _python;

        throw new InvalidOperationException($"Unsupported runtime: {runtimeName}");
    }
}