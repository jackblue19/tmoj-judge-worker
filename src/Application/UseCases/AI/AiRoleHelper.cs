namespace Application.UseCases.AI;

internal static class AiRoleHelper
{
    public static int GetDebugDailyQuota(string? roleCode , int studentQuota , int teacherQuota , int adminQuota)
    {
        roleCode = Normalize(roleCode);

        if ( roleCode is "admin" or "manager" )
            return adminQuota;

        if ( roleCode is "teacher" )
            return teacherQuota;

        return studentQuota;
    }

    public static int GetEditorialDailyQuota(string? roleCode , int teacherQuota , int adminQuota)
    {
        roleCode = Normalize(roleCode);

        if ( roleCode is "admin" or "manager" )
            return adminQuota;

        return teacherQuota;
    }

    public static bool IsTeacherAdminOrManager(string? roleCode)
    {
        roleCode = Normalize(roleCode);
        return roleCode is "teacher" or "admin" or "manager";
    }

    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "student" : value.Trim().ToLowerInvariant();
}