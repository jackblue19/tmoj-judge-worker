using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Constants;

public static class ProblemDifficultyCodes
{
    public const string Easy = "easy";
    public const string Medium = "medium";
    public const string Hard = "hard";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Easy, Medium, Hard
    };
}
