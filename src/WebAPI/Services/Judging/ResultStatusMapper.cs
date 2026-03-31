namespace WebAPI.Services.Judging;

public static class ResultStatusMapper
{
    public static string NormalizeVerdict(string? verdict)
    {
        if ( string.IsNullOrWhiteSpace(verdict) )
            return "ie";

        return verdict.Trim().ToLowerInvariant() switch
        {
            "ac" => "ac",
            "wa" => "wa",
            "tle" => "tle",
            "mle" => "mle",
            "ole" => "ole",
            "re" => "re",
            "rte" => "re",
            "ce" => "ce",
            "ie" => "ie",
            "skipped" => "skipped",
            _ => "ie"
        };
    }
}