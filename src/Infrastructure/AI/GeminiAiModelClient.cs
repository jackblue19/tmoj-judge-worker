using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.AI;
using Application.Common.AI;
using Microsoft.Extensions.Options;

namespace Infrastructure.AI;

public sealed class GeminiAiModelClient : IAiModelClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public GeminiAiModelClient(HttpClient http , IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;

        _http.BaseAddress = new Uri(_options.Gemini.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<AiModelResult> GenerateJsonAsync(
        string model ,
        string systemInstruction ,
        string userPrompt ,
        object responseSchema ,
        AiGenerationSettings settings ,
        CancellationToken ct = default)
    {
        if ( string.IsNullOrWhiteSpace(_options.Gemini.ApiKey) )
            throw new InvalidOperationException("Gemini API key is missing.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = systemInstruction }
                }
            } ,
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = userPrompt }
                    }
                }
            } ,
            generationConfig = new
            {
                temperature = settings.Temperature ,
                topP = settings.TopP ,
                maxOutputTokens = settings.MaxOutputTokens ,
                responseMimeType = settings.ResponseMimeType ,
                responseJsonSchema = responseSchema
            }
        };

        var sw = Stopwatch.StartNew();

        HttpResponseMessage? response = null;
        string body = "";

        for ( var attempt = 1; attempt <= 5; attempt++ )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post ,
                $"models/{model}:generateContent");

            request.Headers.TryAddWithoutValidation("X-goog-api-key" , _options.Gemini.ApiKey);
            request.Content = JsonContent.Create(requestBody);

            response = await _http.SendAsync(request , timeoutCts.Token);
            body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if ( response.IsSuccessStatusCode )
                break;

            if ( !ShouldRetry(response.StatusCode) || attempt == 5 )
                break;

            var baseDelayMs = Math.Pow(2 , attempt - 1) * 1000;
            var jitterMs = Random.Shared.Next(250 , 1000);
            var delay = TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);

            await Task.Delay(delay , timeoutCts.Token);
        }

        sw.Stop();

        if ( response is null )
            throw new InvalidOperationException("Gemini provider returned no response.");

        if ( !response.IsSuccessStatusCode )
        {
            throw new InvalidOperationException(
                $"Gemini provider error: {(int) response.StatusCode} - {body}");
        }

        using var doc = JsonDocument.Parse(body);

        var jsonText =
            doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

        if ( string.IsNullOrWhiteSpace(jsonText) )
            throw new InvalidOperationException("Gemini returned empty response text.");

        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;

        if ( doc.RootElement.TryGetProperty("usageMetadata" , out var usage) )
        {
            if ( usage.TryGetProperty("promptTokenCount" , out var p) )
                promptTokens = p.GetInt32();

            if ( usage.TryGetProperty("candidatesTokenCount" , out var c) )
                completionTokens = c.GetInt32();

            if ( usage.TryGetProperty("totalTokenCount" , out var t) )
                totalTokens = t.GetInt32();
        }

        return new AiModelResult(
            JsonText: jsonText ,
            PromptTokens: promptTokens ,
            CompletionTokens: completionTokens ,
            TotalTokens: totalTokens ,
            LatencyMs: (int) sw.ElapsedMilliseconds);
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}