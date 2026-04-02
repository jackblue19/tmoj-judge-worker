namespace WebAPI.Services.Judging;

public static class ResultStatusMapper
{
    public static string NormalizeVerdict(string? verdict)
    {
        var v = string.IsNullOrWhiteSpace(verdict)
            ? "ie"
            : verdict.Trim().ToLowerInvariant();

        return v switch
        {
            "ac" => "ac",
            "wa" => "wa",
            "tle" => "tle",
            "mle" => "mle",
            "ce" => "ce",
            "re" => "re",
            "rte" => "re",
            "skipped" => "ie",
            "ie" => "ie",
            _ => "ie"
        };
    }
}