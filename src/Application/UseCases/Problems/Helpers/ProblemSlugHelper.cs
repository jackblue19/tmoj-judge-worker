using Domain.Abstractions;

namespace Application.UseCases.Problems.Helpers;

internal static class ProblemSlugHelper
{
    public static string NormalizeSlug(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public static string BuildVirtualSlugBase(string? originSlug)
    {
        var normalized = NormalizeSlug(originSlug);

        if ( string.IsNullOrWhiteSpace(normalized) )
            throw new ArgumentException("Origin problem slug is invalid.");

        return $"{normalized}-virtual";
    }

    public static string BuildRemixSlugBase(string? originSlug)
    {
        var normalized = NormalizeSlug(originSlug);

        if ( string.IsNullOrWhiteSpace(normalized) )
            throw new ArgumentException("Origin problem slug is invalid.");

        return $"{normalized}-remix";
    }

    public static async Task<string> ResolveVirtualSlugAsync(
        string? requestedSlug ,
        string? originSlug ,
        IProblemRepository problemRepository ,
        CancellationToken ct)
    {
        var normalizedRequestedSlug = NormalizeSlug(requestedSlug);

        if ( !string.IsNullOrWhiteSpace(normalizedRequestedSlug) )
        {
            var requestedExists = await problemRepository.SlugExistsAsync(
                normalizedRequestedSlug ,
                null ,
                ct);

            if ( requestedExists )
                throw new InvalidOperationException($"Problem slug '{normalizedRequestedSlug}' already exists.");

            return normalizedRequestedSlug;
        }

        var baseSlug = BuildVirtualSlugBase(originSlug);
        return await ResolveGeneratedSlugAsync(baseSlug , problemRepository , ct);
    }

    public static async Task<string> ResolveRemixSlugAsync(
        string? requestedSlug ,
        string? originSlug ,
        IProblemRepository problemRepository ,
        CancellationToken ct)
    {
        var normalizedRequestedSlug = NormalizeSlug(requestedSlug);

        if ( !string.IsNullOrWhiteSpace(normalizedRequestedSlug) )
        {
            var requestedExists = await problemRepository.SlugExistsAsync(
                normalizedRequestedSlug ,
                null ,
                ct);

            if ( requestedExists )
                throw new InvalidOperationException($"Problem slug '{normalizedRequestedSlug}' already exists.");

            return normalizedRequestedSlug;
        }

        var baseSlug = BuildRemixSlugBase(originSlug);
        return await ResolveGeneratedSlugAsync(baseSlug , problemRepository , ct);
    }

    private static async Task<string> ResolveGeneratedSlugAsync(
        string baseSlug ,
        IProblemRepository problemRepository ,
        CancellationToken ct)
    {
        var baseExists = await problemRepository.SlugExistsAsync(baseSlug , null , ct);
        if ( !baseExists )
            return baseSlug;

        var index = 2;
        while ( true )
        {
            var candidate = $"{baseSlug}-{index}";
            var exists = await problemRepository.SlugExistsAsync(candidate , null , ct);

            if ( !exists )
                return candidate;

            index++;
        }
    }
}