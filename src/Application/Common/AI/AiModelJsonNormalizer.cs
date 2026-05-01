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

        var looksLikeTruncatedJson = cleaned.StartsWith("{") || cleaned.StartsWith("[");

        if ( featureCode == "editorial_draft" )
        {
            var safeContent = looksLikeTruncatedJson
                ? """
                # AI Editorial Draft

                AI provider returned an incomplete JSON response, so the draft could not be safely parsed.

                Please regenerate the draft. If this happens repeatedly, reduce statement/sample size or lower MaxOutputTokens.
                """
                : cleaned;

            return JsonSerializer.Serialize(new
            {
                title = fallbackTitle ,
                summaryMd = fallbackSummary ,
                contentMd = safeContent ,
                confidence = 20 ,
                confidenceLevelCode = "low" ,
                warnings = new[]
                {
                    "AI response was incomplete or invalid JSON.",
                    "Teacher review is required before publishing."
                } ,
                assumptions = Array.Empty<string>()
            });
        }

        var safeDebugContent = looksLikeTruncatedJson
            ? """
            - AI provider returned an incomplete JSON response.
            - The explanation could not be safely parsed.
            - Please retry with lower MaxOutputTokens or shorter context.
            """
            : cleaned;

        return JsonSerializer.Serialize(new
        {
            summary = fallbackSummary ,
            suspectedIssueCode = "unknown" ,
            confidence = 20 ,
            confidenceLevelCode = "low" ,
            sections = new[]
            {
                new
                {
                    title = "AI response incomplete",
                    contentMd = safeDebugContent
                }
            } ,
            safetyNote = "AI có thể giải thích sai. Hãy dùng như gợi ý hỗ trợ, không thay thế kết quả judge."
        });
    }

    private static string StripMarkdownFence(string value)
    {
        if ( value.StartsWith("```json" , StringComparison.OrdinalIgnoreCase) )
            value = value["```json".Length..].Trim();
        else if ( value.StartsWith("```" , StringComparison.OrdinalIgnoreCase) )
            value = value["```".Length..].Trim();

        if ( value.EndsWith("```" , StringComparison.OrdinalIgnoreCase) )
            value = value[..^3].Trim();

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