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

    public ICpExecutorProfile Resolve(string runtimeProfileKey)
    {
        if ( string.IsNullOrWhiteSpace(runtimeProfileKey) )
            throw new InvalidOperationException("Runtime profile key is required.");

        if ( _profiles.TryGetValue(runtimeProfileKey.Trim() , out var profile) )
            return profile;

        throw new InvalidOperationException($"Unsupported runtime profile key: {runtimeProfileKey}");
    }
}