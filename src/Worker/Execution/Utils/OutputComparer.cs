namespace Worker.Execution.Utils;

public static class OutputComparer
{
    public static bool Compare(string expected , string actual)
    {
        var normExpected = Normalize(expected);
        var normActual = Normalize(actual);

        return normExpected == normActual;
    }

    private static string Normalize(string s)
    {
        return string.Join('\n' ,
            s.Replace("\r\n" , "\n")
             .Split('\n')
             .Select(line => line.TrimEnd()))
             .Trim();
    }
}