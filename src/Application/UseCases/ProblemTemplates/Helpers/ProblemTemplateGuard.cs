using Domain.Constants;

namespace Application.UseCases.ProblemTemplates.Helpers;

internal static class ProblemTemplateGuard
{
    public static void ValidateCreateInput(
        Guid problemId ,
        Guid runtimeId ,
        string? templateCode ,
        int? version)
    {
        if ( problemId == Guid.Empty )
            throw new ArgumentException("ProblemId is required.");

        if ( runtimeId == Guid.Empty )
            throw new ArgumentException("RuntimeId is required.");

        if ( string.IsNullOrWhiteSpace(templateCode) )
            throw new ArgumentException("TemplateCode is required.");

        if ( version.HasValue && version.Value <= 0 )
            throw new ArgumentException("Version must be greater than 0 when provided.");
    }

    public static void ValidateUpdateInput(Guid codeTemplateId , string? templateCode)
    {
        if ( codeTemplateId == Guid.Empty )
            throw new ArgumentException("CodeTemplateId is required.");

        if ( string.IsNullOrWhiteSpace(templateCode) )
            throw new ArgumentException("TemplateCode is required.");
    }

    public static string NormalizeInjectionPoint(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "{{USER_CODE}}"
            : value.Trim();
    }

    public static int NormalizeVersion(int? value)
    {
        return value is null or <= 0 ? 1 : value.Value;
    }

    public static string ResolveWrapperTypeFromProblemMode(string? problemMode)
    {
        var normalized = problemMode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            ProblemModeCodes.Amateur => "function_only",
            ProblemModeCodes.Pro => "full",
            null or "" => "full",
            _ => throw new ArgumentException("Invalid problem mode.")
        };
    }
}