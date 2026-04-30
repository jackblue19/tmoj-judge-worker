namespace Application.Common.AI;

public sealed class AiOptions
{
    public string Provider { get; set; } = "gemini";

    public AiDebugOptions Debug { get; set; } = new();

    public AiEditorialOptions Editorial { get; set; } = new();

    public GeminiOptions Gemini { get; set; } = new();

    public OpenAiOptions OpenAI { get; set; } = new();

}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public sealed class AiDebugOptions
{
    public string Model { get; set; } = "gemini-2.5-flash";
    public string PromptVersion { get; set; } = "debug-v1";
    public string LanguageCode { get; set; } = "vi";

    public double Temperature { get; set; } = 0.2;
    public double TopP { get; set; } = 0.8;
    public int MaxOutputTokens { get; set; } = 1200;
    public int TimeoutSeconds { get; set; } = 90;
    public string ResponseMimeType { get; set; } = "application/json";

    public int DailyQuotaPerStudent { get; set; } = 10;
    public int DailyQuotaPerTeacher { get; set; } = 20;
    public int DailyQuotaPerAdmin { get; set; } = 100;

    public int MaxProblemStatementChars { get; set; } = 4000;
    public int MaxSourceCodeChars { get; set; } = 12000;
    public int MaxTestcaseChars { get; set; } = 4000;
    public int MaxMessageChars { get; set; } = 3000;

    public bool UseCache { get; set; } = true;
    public bool AllowFullSolution { get; set; } = false;
    public bool AllowTeacherSolutionForStudent { get; set; } = false;
}

public sealed class AiEditorialOptions
{
    public string Model { get; set; } = "gemini-2.5-flash";
    public string PromptVersion { get; set; } = "editorial-v1";
    public string LanguageCode { get; set; } = "vi";
    public string StyleCode { get; set; } = "educational";
    public string TargetAudienceCode { get; set; } = "student";

    public double Temperature { get; set; } = 0.35;
    public double TopP { get; set; } = 0.85;
    public int MaxOutputTokens { get; set; } = 2500;
    public int TimeoutSeconds { get; set; } = 90;
    public string ResponseMimeType { get; set; } = "application/json";

    public int DailyQuotaPerTeacher { get; set; } = 20;
    public int DailyQuotaPerAdmin { get; set; } = 100;

    public int MaxProblemStatementChars { get; set; } = 12000;
    public int MaxSampleTestcasesChars { get; set; } = 6000;
    public int MaxTeacherSolutionChars { get; set; } = 8000;

    public bool IncludePseudocode { get; set; } = true;
    public bool IncludeCorrectness { get; set; } = true;
    public bool IncludeComplexity { get; set; } = true;

    public bool UseCache { get; set; } = true;
    public bool AllowAutoPublish { get; set; } = false;
}

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string? FallbackModel { get; set; }
}