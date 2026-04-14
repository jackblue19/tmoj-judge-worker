using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Constants;

internal class PublicProblemConstants
{
}

public static class ProblemStatus
{
    public const string Published = "published";
}

public static class ProblemVisibility
{
    public const string Public = "public";
}

public static class TestsetType
{
    public const string Primary = "primary";
}

public static class Roles
{
    public static readonly string[] AdminRoles =
    {
        "admin", "teacher", "manager", "Admin"
    };
}
