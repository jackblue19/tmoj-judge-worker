using System.Text.Json;
using Application.UseCases.AI.Dtos;

namespace Application.UseCases.AI;

internal static class AiJsonReader
{
    public static string ReadString(JsonElement root , string name , string fallback)
        => root.TryGetProperty(name , out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    public static int ReadInt(JsonElement root , string name , int fallback)
        => root.TryGetProperty(name , out var value) && value.TryGetInt32(out var result)
            ? result
            : fallback;

    public static IReadOnlyList<AiDebugSectionDto> ReadDebugSections(JsonElement root)
    {
        if ( !root.TryGetProperty("sections" , out var sections) || sections.ValueKind != JsonValueKind.Array )
            return Array.Empty<AiDebugSectionDto>();

        return sections.EnumerateArray()
            .Select(x => new AiDebugSectionDto(
                ReadString(x , "title" , "Section") ,
                ReadString(x , "contentMd" , "")))
            .ToList();
    }

    public static IReadOnlyList<string> ReadStringArray(JsonElement root , string name)
    {
        if ( !root.TryGetProperty(name , out var arr) || arr.ValueKind != JsonValueKind.Array )
            return Array.Empty<string>();

        return arr.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    public static IReadOnlyList<string> ReadStringArrayFromJson(string? json)
    {
        if ( string.IsNullOrWhiteSpace(json) )
            return Array.Empty<string>();

        using var doc = JsonDocument.Parse(json);

        if ( doc.RootElement.ValueKind != JsonValueKind.Array )
            return Array.Empty<string>();

        return doc.RootElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}