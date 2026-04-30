namespace Application.Abstractions.AI;

public interface IAiModelClient
{
    Task<AiModelResult> GenerateJsonAsync(
        string model ,
        string systemInstruction ,
        string userPrompt ,
        object responseSchema ,
        AiGenerationSettings settings ,
        CancellationToken ct = default);
}

public sealed record AiModelResult(
    string JsonText ,
    int? PromptTokens ,
    int? CompletionTokens ,
    int? TotalTokens ,
    int LatencyMs);

public sealed record AiGenerationSettings(
    double Temperature ,
    double TopP ,
    int MaxOutputTokens ,
    int TimeoutSeconds ,
    string ResponseMimeType);