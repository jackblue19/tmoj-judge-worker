using Application.UseCases.Problems.Constants;
using Domain.Constants;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Application.UseCases.Problems.Helpers;

public static class ProblemCommandGuard
{
    public static void ValidateStatementInput(string? descriptionMd , IFormFile? statementFile)
    {
        var hasDescription = !string.IsNullOrWhiteSpace(descriptionMd);
        var hasFile = statementFile is not null && statementFile.Length > 0;

        if ( !hasDescription && !hasFile )
            throw new ArgumentException("Either description markdown or statement file is required.");

        if ( hasDescription && hasFile )
            throw new ArgumentException("Description markdown and statement file cannot be provided together.");
    }

    public static (string ext, string contentType, string sourceCode, string originalFileName) ResolveStatement(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var originalFileName = Path.GetFileName(file.FileName);

        return ext switch
        {
            ".md" => (".md", "text/markdown; charset=utf-8", ProblemStatementSourceCodes.InlineMd, originalFileName),
            ".pdf" => (".pdf", "application/pdf", ProblemStatementSourceCodes.R2Pdf, originalFileName),
            _ => throw new ArgumentException("Statement file must be .md or .pdf.")
        };
    }

    public static async Task<string> ReadMarkdownFileAsync(IFormFile file , CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream , Encoding.UTF8 , detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(ct);
    }
    public static string? NormalizeVisibilityCode(string? visibilityCode)
    {
        var normalized = visibilityCode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => null,
            "public" => "public",
            "private" => "private",
            "in-class" => "in-class",
            "in-bank" => "in-bank",
            "in-plan" => "in-plan",
            _ => throw new ArgumentException("Invalid visibility code. Allowed values: public, private, in-class, in-bank, in-plan.")
        };
    }

    public static string? NormalizeDifficulty(string? difficulty)
    {
        var normalized = difficulty?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => null,
            "easy" => "easy",
            "medium" => "medium",
            "hard" => "hard",
            _ => throw new ArgumentException("Invalid difficulty. Allowed values: easy, medium, hard.")
        };
    }

    public static string NormalizeProblemMode(string? problemMode)
    {
        var normalized = problemMode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => ProblemModeCodes.Pro,
            ProblemModeCodes.Amateur => ProblemModeCodes.Amateur,
            ProblemModeCodes.Pro => ProblemModeCodes.Pro,
            _ => throw new ArgumentException("Invalid problem mode. Allowed values: amateur, pro.")
        };
    }

    public static IReadOnlyList<Guid> NormalizeTagIds(IReadOnlyCollection<Guid>? tagIds)
    {
        if ( tagIds is null || tagIds.Count == 0 )
            return [];

        return tagIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
    }

    public static string NormalizeVisibilityForVirtual(string? visibilityCode)
    {
        var normalized = visibilityCode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => "private",
            "public" => "public",
            "private" => "private",
            "in-class" => "in-class",
            "in-bank" => "in-bank",
            _ => throw new ArgumentException("Invalid visibility code for virtual problem.")
        };
    }

    public static string NormalizeVisibilityForClone(string? visibilityCode , string? fallback)
    {
        var normalized = visibilityCode?.Trim().ToLowerInvariant();

        if ( string.IsNullOrWhiteSpace(normalized) )
            return string.IsNullOrWhiteSpace(fallback) ? ProblemVisibilityCodes.Private : fallback.Trim().ToLowerInvariant();

        return normalized switch
        {
            ProblemVisibilityCodes.Public => ProblemVisibilityCodes.Public,
            ProblemVisibilityCodes.Private => ProblemVisibilityCodes.Private,
            ProblemVisibilityCodes.InClass => ProblemVisibilityCodes.InClass,
            ProblemVisibilityCodes.InBank => ProblemVisibilityCodes.InBank,
            _ => throw new ArgumentException("Invalid visibility code.")
        };
    }

    public static void ValidateProblemCoreFields(
        string? title ,
        string? slug ,
        int? timeLimitMs ,
        int? memoryLimitKb)
    {
        if ( string.IsNullOrWhiteSpace(title) )
            throw new ArgumentException("Title is required.");

        if ( string.IsNullOrWhiteSpace(slug) )
            throw new ArgumentException("Slug is required.");

        if ( timeLimitMs.HasValue && timeLimitMs.Value <= 0 )
            throw new ArgumentException("TimeLimitMs must be greater than 0 when provided.");

        if ( memoryLimitKb.HasValue && memoryLimitKb.Value <= 0 )
            throw new ArgumentException("MemoryLimitKb must be greater than 0 when provided.");
    }
}