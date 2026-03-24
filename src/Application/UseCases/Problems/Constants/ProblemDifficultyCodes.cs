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
