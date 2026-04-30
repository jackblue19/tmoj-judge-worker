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

        // Important:
        // We manage timeout by CancellationTokenSource per request.
        // Avoid HttpClient timeout fighting with our retry/backoff flow.
        _http.Timeout = Timeout.InfiniteTimeSpan;
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
                responseSchema = responseSchema
            }
        };

        try
        {
            var primary = await SendWithRetryAsync(
                model ,
                requestBody ,
                settings ,
                timeoutCts.Token);

            if ( primary.IsSuccess )
                return primary.Result!;

            var fallbackModel = _options.Gemini.FallbackModel;

            if ( !string.IsNullOrWhiteSpace(fallbackModel)
                && !string.Equals(fallbackModel , model , StringComparison.OrdinalIgnoreCase)
                && primary.ShouldFallback )
            {
                var fallback = await SendWithRetryAsync(
                    fallbackModel ,
                    requestBody ,
                    settings ,
                    timeoutCts.Token);

                if ( fallback.IsSuccess )
                    return fallback.Result!;

                throw new InvalidOperationException(fallback.ErrorMessage);
            }

            throw new InvalidOperationException(primary.ErrorMessage);
        }
        catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
        {
            throw new TimeoutException(
                $"Gemini request timed out after {settings.TimeoutSeconds} seconds.");
        }
    }

    public async Task<AiModelResult> GenerateTextAsync(
        string model ,
        string systemInstruction ,
        string userPrompt ,
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
                maxOutputTokens = settings.MaxOutputTokens
            }
        };

        try
        {
            var primary = await SendWithRetryAsync(
                model ,
                requestBody ,
                settings ,
                timeoutCts.Token);

            if ( primary.IsSuccess )
                return primary.Result!;

            var fallbackModel = _options.Gemini.FallbackModel;

            if ( !string.IsNullOrWhiteSpace(fallbackModel)
                && !string.Equals(fallbackModel , model , StringComparison.OrdinalIgnoreCase)
                && primary.ShouldFallback )
            {
                var fallback = await SendWithRetryAsync(
                    fallbackModel ,
                    requestBody ,
                    settings ,
                    timeoutCts.Token);

                if ( fallback.IsSuccess )
                    return fallback.Result!;

                throw new InvalidOperationException(fallback.ErrorMessage);
            }

            throw new InvalidOperationException(primary.ErrorMessage);
        }
        catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
        {
            throw new TimeoutException(
                $"Gemini request timed out after {settings.TimeoutSeconds} seconds.");
        }
    }

    private async Task<GeminiAttemptResult> SendWithRetryAsync(
        string model ,
        object requestBody ,
        AiGenerationSettings settings ,
        CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        string body = "";
        var sw = Stopwatch.StartNew();

        // Editorial often uses more tokens, so give it one extra attempt.
        var maxAttempts = settings.MaxOutputTokens >= 3000 ? 4 : 3;

        for ( var attempt = 1; attempt <= maxAttempts; attempt++ )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post ,
                $"models/{model}:generateContent");

            // Do not put API key in query string, otherwise logs may expose it.
            request.Headers.TryAddWithoutValidation("X-goog-api-key" , _options.Gemini.ApiKey);
            request.Content = JsonContent.Create(requestBody);

            response = await _http.SendAsync(request , ct);
            body = await response.Content.ReadAsStringAsync(ct);

            if ( response.IsSuccessStatusCode )
                break;

            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                break;

            if ( !ShouldRetry(response.StatusCode) || attempt == maxAttempts )
                break;

            var delayMs = Math.Min(
                5000 ,
                (int) Math.Pow(2 , attempt - 1) * 750 + Random.Shared.Next(250 , 1000));

            await Task.Delay(TimeSpan.FromMilliseconds(delayMs) , ct);
        }

        sw.Stop();

        if ( response is null )
        {
            return GeminiAttemptResult.Failed(
                "Gemini provider returned no response." ,
                shouldFallback: true);
        }

        if ( !response.IsSuccessStatusCode )
        {
            var shouldFallback = response.StatusCode is
                HttpStatusCode.TooManyRequests
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.BadGateway
                or HttpStatusCode.GatewayTimeout;

            return GeminiAttemptResult.Failed(
                $"Gemini provider error: {(int) response.StatusCode} - {body}" ,
                shouldFallback);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);

            var text =
                doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            if ( string.IsNullOrWhiteSpace(text) )
            {
                return GeminiAttemptResult.Failed(
                    "Gemini returned empty response text." ,
                    shouldFallback: true);
            }

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

            return GeminiAttemptResult.Success(new AiModelResult(
                JsonText: text ,
                PromptTokens: promptTokens ,
                CompletionTokens: completionTokens ,
                TotalTokens: totalTokens ,
                LatencyMs: (int) sw.ElapsedMilliseconds));
        }
        catch ( Exception ex )
        {
            return GeminiAttemptResult.Failed(
                $"Failed to parse Gemini provider response: {ex.Message}" ,
                shouldFallback: true);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private sealed record GeminiAttemptResult(
        bool IsSuccess ,
        AiModelResult? Result ,
        string ErrorMessage ,
        bool ShouldFallback)
    {
        public static GeminiAttemptResult Success(AiModelResult result)
            => new(true , result , string.Empty , false);

        public static GeminiAttemptResult Failed(string errorMessage , bool shouldFallback)
            => new(false , null , errorMessage , shouldFallback);
    }
}