using Microsoft.Extensions.Configuration;

namespace Worker.Execution.Testset;

public sealed class TestsetPathResolver
{
    private readonly string _problemsRoot;

    public TestsetPathResolver(IConfiguration configuration)
    {
        _problemsRoot =
            configuration["Judge:ProblemsRoot"]
            ?? "/var/lib/tmoj/problems";
    }

    public string GetTestsetRoot(string slug , Guid testsetId)
    {
        return Path.Combine(_problemsRoot , SanitizeFolderName(slug) , testsetId.ToString());
    }

    public string GetReadyFile(string slug , Guid testsetId)
    {
        return Path.Combine(GetTestsetRoot(slug , testsetId) , ".ready");
    }

    public string GetOrdinalFolder(string slug , Guid testsetId , int ordinal)
    {
        return Path.Combine(GetTestsetRoot(slug , testsetId) , ordinal.ToString("000"));
    }

    public string GetCanonicalInputPath(string slug , Guid testsetId , int ordinal)
    {
        return Path.Combine(GetOrdinalFolder(slug , testsetId , ordinal) , $"{slug}.inp");
    }

    public string GetCanonicalOutputPath(string slug , Guid testsetId , int ordinal)
    {
        return Path.Combine(GetOrdinalFolder(slug , testsetId , ordinal) , $"{slug}.out");
    }

    public bool IsReady(string slug , Guid testsetId)
    {
        return File.Exists(GetReadyFile(slug , testsetId));
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Trim();
    }
}