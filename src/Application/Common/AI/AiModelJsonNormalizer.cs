using System.Text.Json;

namespace Application.Common.AI;

public static class AiModelJsonNormalizer
{
    public static string NormalizeOrFallback(
        string raw ,
        string featureCode ,
        string fallbackTitle ,
        string fallbackSummary)
    {
        raw ??= string.Empty;

        var cleaned = StripMarkdownFence(raw.Trim());

        if ( TryParseObject(cleaned , out _) )
            return cleaned;

        var extracted = ExtractFirstJsonObject(cleaned);

        if ( !string.IsNullOrWhiteSpace(extracted) && TryParseObject(extracted , out _) )
            return extracted;

        if ( featureCode == "editorial_draft" )
        {
            return JsonSerializer.Serialize(new
            {
                title = fallbackTitle ,
                summaryMd = fallbackSummary ,
                contentMd = cleaned ,
                confidence = 30 ,
                confidenceLevelCode = "low" ,
                outline = new
                {
                    sections = new[]
                    {
                        "AI Raw Draft"
                    }
                } ,
                warnings = new[]
                {
                    "AI response was not valid JSON. Backend wrapped the raw response as a draft.",
                    "Teacher review is required before publishing."
                } ,
                assumptions = Array.Empty<string>()
            });
        }

        return JsonSerializer.Serialize(new
        {
            summary = fallbackSummary ,
            suspectedIssueCode = "unknown" ,
            confidence = 30 ,
            confidenceLevelCode = "low" ,
            sections = new[]
            {
                new
                {
                    title = "AI Raw Explanation",
                    contentMd = cleaned
                }
            } ,
            safetyNote = "AI có thể giải thích sai. Hãy dùng như gợi ý hỗ trợ, không thay thế kết quả judge."
        });
    }

    private static string StripMarkdownFence(string value)
    {
        if ( value.StartsWith("```json" , StringComparison.OrdinalIgnoreCase) )
        {
            value = value["```json".Length..].Trim();
        }
        else if ( value.StartsWith("```" , StringComparison.OrdinalIgnoreCase) )
        {
            value = value["```".Length..].Trim();
        }

        if ( value.EndsWith("```" , StringComparison.OrdinalIgnoreCase) )
        {
            value = value[..^3].Trim();
        }

        return value;
    }

    private static bool TryParseObject(string value , out JsonDocument? doc)
    {
        doc = null;

        if ( string.IsNullOrWhiteSpace(value) )
            return false;

        try
        {
            doc = JsonDocument.Parse(value);

            if ( doc.RootElement.ValueKind != JsonValueKind.Object )
            {
                doc.Dispose();
                doc = null;
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractFirstJsonObject(string value)
    {
        var start = value.IndexOf('{');
        if ( start < 0 )
            return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for ( var i = start; i < value.Length; i++ )
        {
            var c = value[i];

            if ( escaped )
            {
                escaped = false;
                continue;
            }

            if ( c == '\\' && inString )
            {
                escaped = true;
                continue;
            }

            if ( c == '"' )
            {
                inString = !inString;
                continue;
            }

            if ( inString )
                continue;

            if ( c == '{' )
                depth++;

            if ( c == '}' )
            {
                depth--;

                if ( depth == 0 )
                    return value[start..(i + 1)];
            }
        }

        return null;
    }
}