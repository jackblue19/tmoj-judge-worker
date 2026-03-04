using System.Text;

namespace WebAPI.Extensions;

public static class StoragePathHelper
{
    public static string SanitizeFolderName(string input)
    {
        if ( string.IsNullOrWhiteSpace(input) )
            return "unknown";

        var invalid = Path.GetInvalidFileNameChars();

        var sb = new StringBuilder(input.Length);
        foreach ( var ch in input.Trim() )
        {
            if ( invalid.Contains(ch) )
                continue;

            if ( ch == ':' || ch == '/' || ch == '\\' )
                continue;

            sb.Append(ch);
        }

        var s = sb.ToString().Trim();

        if ( string.IsNullOrWhiteSpace(s) )
            return "unknown";

        while ( s.Contains(".." , StringComparison.Ordinal) )
            s = s.Replace(".." , "." , StringComparison.Ordinal);

        return s;
    }
}