namespace Worker.Execution.Utils;

public static class OutputComparer
{
    public static bool Compare(string expected , string actual , string compareMode = "trim")
    {
        compareMode = string.IsNullOrWhiteSpace(compareMode)
            ? "trim"
            : compareMode.Trim().ToLowerInvariant();

        return compareMode switch
        {
            "exact" => NormalizeExact(expected) == NormalizeExact(actual),
            "token" => NormalizeToken(expected) == NormalizeToken(actual),
            "ignore_whitespace" => NormalizeToken(expected) == NormalizeToken(actual),
            "trim" => NormalizeTrim(expected) == NormalizeTrim(actual),
            _ => NormalizeTrim(expected) == NormalizeTrim(actual)
        };
    }

    public static string NormalizeForNote(string value)
        => NormalizeTrim(value);

    private static string NormalizeExact(string s)
        => s.Replace("\r\n" , "\n").Replace("\r" , "\n");

    private static string NormalizeTrim(string s)
    {
        return string.Join('\n' ,
            s.Replace("\r\n" , "\n")
             .Replace("\r" , "\n")
             .Split('\n')
             .Select(line => line.TrimEnd()))
             .Trim();
    }

    private static string NormalizeToken(string s)
    {
        var tokens = s.Replace("\r\n" , "\n")
            .Replace("\r" , "\n")
            .Split((char[]?) null , StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ' , tokens);
    }
}