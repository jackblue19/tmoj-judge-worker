namespace WebAPI.Services.Judging;

public static class ResultStatusMapper
{
    public static string NormalizeVerdict(string? verdict)
    {
        var value = string.IsNullOrWhiteSpace(verdict)
            ? "ie"
            : verdict.Trim().ToLowerInvariant();

        return value switch
        {
            "accepted" => "ac",
            "wrong_answer" => "wa",
            "time_limit_exceeded" => "tle",
            "memory_limit_exceeded" => "mle",
            "compile_error" => "ce",
            "runtime_error" => "re",

            "ac" => "ac",
            "wa" => "wa",
            "tle" => "tle",
            "mle" => "mle",
            "ce" => "ce",
            "re" => "re",
            "rte" => "re",
            "ie" => "ie",

            "skipped" => "ie",
            "failed" => "ie",
            "error" => "ie",

            _ => "ie"
        };
    }
}