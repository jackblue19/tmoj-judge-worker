using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.AI;
using Application.Common.AI;
using Microsoft.Extensions.Options;

namespace Infrastructure.AI;

public sealed class OpenAiModelClient : IAiModelClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiModelClient(HttpClient http , IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;

        _http.BaseAddress = new Uri(_options.OpenAI.BaseUrl.TrimEnd('/') + "/");
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
        if ( string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey) )
            throw new InvalidOperationException("OpenAI API key is missing.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        var requestBody = new
        {
            model ,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = systemInstruction
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = userPrompt
                        }
                    }
                }
            } ,
            temperature = settings.Temperature ,
            top_p = settings.TopP ,
            max_output_tokens = settings.MaxOutputTokens ,
            text = new
            {
                format = new
                {
                    type = "json_schema" ,
                    name = "tmoj_ai_response" ,
                    strict = true ,
                    schema = NormalizeSchemaForOpenAi(responseSchema)
                }
            }
        };

        try
        {
            var attempt = await SendWithRetryAsync(
                requestBody ,
                settings ,
                timeoutCts.Token);

            if ( attempt.IsSuccess )
                return attempt.Result!;

            throw new InvalidOperationException(attempt.ErrorMessage);
        }
        catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
        {
            throw new TimeoutException(
                $"OpenAI request timed out after {settings.TimeoutSeconds} seconds.");
        }
    }

    public async Task<AiModelResult> GenerateTextAsync(
        string model ,
        string systemInstruction ,
        string userPrompt ,
        AiGenerationSettings settings ,
        CancellationToken ct = default)
    {
        if ( string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey) )
            throw new InvalidOperationException("OpenAI API key is missing.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        var requestBody = new
        {
            model ,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = systemInstruction
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = userPrompt
                        }
                    }
                }
            } ,
            temperature = settings.Temperature ,
            top_p = settings.TopP ,
            max_output_tokens = settings.MaxOutputTokens
        };

        try
        {
            var attempt = await SendWithRetryAsync(
                requestBody ,
                settings ,
                timeoutCts.Token);

            if ( attempt.IsSuccess )
                return attempt.Result!;

            throw new InvalidOperationException(attempt.ErrorMessage);
        }
        catch ( OperationCanceledException ) when ( !ct.IsCancellationRequested )
        {
            throw new TimeoutException(
                $"OpenAI request timed out after {settings.TimeoutSeconds} seconds.");
        }
    }

    private async Task<OpenAiAttemptResult> SendWithRetryAsync(
        object requestBody ,
        AiGenerationSettings settings ,
        CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        string body = "";
        var sw = Stopwatch.StartNew();

        var maxAttempts = settings.MaxOutputTokens >= 3000 ? 3 : 2;

        for ( var attempt = 1; attempt <= maxAttempts; attempt++ )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post ,
                "responses");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer" ,
                    _options.OpenAI.ApiKey);

            request.Content = JsonContent.Create(requestBody);

            response = await _http.SendAsync(request , ct);
            body = await response.Content.ReadAsStringAsync(ct);

            if ( response.IsSuccessStatusCode )
                break;

            // Do not aggressively retry quota/rate-limit errors.
            if ( response.StatusCode == HttpStatusCode.TooManyRequests )
                break;

            if ( !ShouldRetry(response.StatusCode) || attempt == maxAttempts )
                break;

            var delayMs = Math.Min(
                4000 ,
                (int) Math.Pow(2 , attempt - 1) * 750 + Random.Shared.Next(250 , 1000));

            await Task.Delay(TimeSpan.FromMilliseconds(delayMs) , ct);
        }

        sw.Stop();

        if ( response is null )
        {
            return OpenAiAttemptResult.Failed(
                "OpenAI provider returned no response.");
        }

        if ( !response.IsSuccessStatusCode )
        {
            return OpenAiAttemptResult.Failed(
                $"OpenAI provider error: {(int) response.StatusCode} - {body}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);

            var outputText = ExtractOutputText(doc.RootElement);

            if ( string.IsNullOrWhiteSpace(outputText) )
            {
                return OpenAiAttemptResult.Failed(
                    "OpenAI returned empty output text.");
            }

            var usage = doc.RootElement.TryGetProperty("usage" , out var usageEl)
                ? usageEl
                : default;

            int? promptTokens = null;
            int? completionTokens = null;
            int? totalTokens = null;

            if ( usage.ValueKind == JsonValueKind.Object )
            {
                if ( usage.TryGetProperty("input_tokens" , out var inputTokens) )
                    promptTokens = inputTokens.GetInt32();

                if ( usage.TryGetProperty("output_tokens" , out var outputTokens) )
                    completionTokens = outputTokens.GetInt32();

                if ( usage.TryGetProperty("total_tokens" , out var total) )
                    totalTokens = total.GetInt32();
            }

            return OpenAiAttemptResult.Success(new AiModelResult(
                JsonText: outputText ,
                PromptTokens: promptTokens ,
                CompletionTokens: completionTokens ,
                TotalTokens: totalTokens ,
                LatencyMs: (int) sw.ElapsedMilliseconds));
        }
        catch ( Exception ex )
        {
            return OpenAiAttemptResult.Failed(
                $"Failed to parse OpenAI provider response: {ex.Message}");
        }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if ( root.TryGetProperty("output_text" , out var outputText)
            && outputText.ValueKind == JsonValueKind.String )
        {
            return outputText.GetString() ?? string.Empty;
        }

        if ( !root.TryGetProperty("output" , out var output)
            || output.ValueKind != JsonValueKind.Array )
        {
            return string.Empty;
        }

        foreach ( var item in output.EnumerateArray() )
        {
            if ( !item.TryGetProperty("content" , out var content)
                || content.ValueKind != JsonValueKind.Array )
            {
                continue;
            }

            foreach ( var contentItem in content.EnumerateArray() )
            {
                if ( contentItem.TryGetProperty("text" , out var text)
                    && text.ValueKind == JsonValueKind.String )
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static object NormalizeSchemaForOpenAi(object schema)
    {
        var json = JsonSerializer.Serialize(schema);
        using var doc = JsonDocument.Parse(json);

        var normalized = NormalizeSchemaElement(doc.RootElement);

        return normalized;
    }

    private static object NormalizeSchemaElement(JsonElement element)
    {
        if ( element.ValueKind != JsonValueKind.Object )
            return JsonSerializer.Deserialize<object>(element.GetRawText())!;

        var dict = new Dictionary<string , object?>();

        foreach ( var prop in element.EnumerateObject() )
        {
            if ( prop.NameEquals("description") )
                continue;

            if ( prop.NameEquals("minItems") )
                continue;

            if ( prop.NameEquals("maxItems") )
                continue;

            if ( prop.NameEquals("properties") && prop.Value.ValueKind == JsonValueKind.Object )
            {
                var properties = new Dictionary<string , object?>();

                foreach ( var child in prop.Value.EnumerateObject() )
                {
                    properties[child.Name] = NormalizeSchemaElement(child.Value);
                }

                dict[prop.Name] = properties;
                continue;
            }

            if ( prop.NameEquals("items") )
            {
                dict[prop.Name] = NormalizeSchemaElement(prop.Value);
                continue;
            }

            if ( prop.Value.ValueKind == JsonValueKind.Object )
            {
                dict[prop.Name] = NormalizeSchemaElement(prop.Value);
                continue;
            }

            if ( prop.Value.ValueKind == JsonValueKind.Array )
            {
                dict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                continue;
            }

            dict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
        }

        if ( dict.TryGetValue("type" , out var typeObj)
            && string.Equals(Convert.ToString(typeObj) , "object" , StringComparison.OrdinalIgnoreCase)
            && dict.TryGetValue("properties" , out var schemaPropertiesObj)
            && schemaPropertiesObj is Dictionary<string , object?> schemaProperties )
        {
            if ( !dict.ContainsKey("required") )
                dict["required"] = schemaProperties.Keys.ToArray();

            if ( !dict.ContainsKey("additionalProperties") )
                dict["additionalProperties"] = false;
        }

        return dict;
    }

    private sealed record OpenAiAttemptResult(
        bool IsSuccess ,
        AiModelResult? Result ,
        string ErrorMessage)
    {
        public static OpenAiAttemptResult Success(AiModelResult result)
            => new(true , result , string.Empty);

        public static OpenAiAttemptResult Failed(string errorMessage)
            => new(false , null , errorMessage);
    }
}