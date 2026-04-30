using System.Text.Json;
using Application.Abstractions.AI;
using Application.Common.AI;
using Application.UseCases.AI.Dtos;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.UseCases.AI;

public sealed record GenerateAiEditorialDraftCommand(
    Guid ProblemId ,
    Guid CurrentUserId ,
    string? LanguageCode ,
    string? StyleCode ,
    string? TargetAudienceCode ,
    bool? IncludePseudocode ,
    bool? IncludeCorrectness ,
    bool? IncludeComplexity) : IRequest<AiEditorialDraftResponseDto>;

public sealed class GenerateAiEditorialDraftCommandHandler
    : IRequestHandler<GenerateAiEditorialDraftCommand , AiEditorialDraftResponseDto>
{
    private const string FeatureCode = "editorial_draft";
    private const string ResponseMode = "markdown";
    private const string EndMarker = "END_OF_EDITORIAL";

    private readonly IAiModelClient _ai;
    private readonly IAiEditorialDataService _data;
    private readonly AiOptions _options;

    public GenerateAiEditorialDraftCommandHandler(
        IAiModelClient ai ,
        IAiEditorialDataService data ,
        IOptions<AiOptions> options)
    {
        _ai = ai;
        _data = data;
        _options = options.Value;
    }

    public async Task<AiEditorialDraftResponseDto> Handle(
        GenerateAiEditorialDraftCommand request ,
        CancellationToken ct)
    {
        var cfg = _options.Editorial;

        var languageCode = Normalize(request.LanguageCode , cfg.LanguageCode);
        var styleCode = Normalize(request.StyleCode , cfg.StyleCode);
        var targetAudienceCode = Normalize(request.TargetAudienceCode , cfg.TargetAudienceCode);

        var includePseudocode = request.IncludePseudocode ?? cfg.IncludePseudocode;
        var includeCorrectness = request.IncludeCorrectness ?? cfg.IncludeCorrectness;
        var includeComplexity = request.IncludeComplexity ?? cfg.IncludeComplexity;

        var roleCode = await _data.GetCurrentUserRoleCodeAsync(
            request.CurrentUserId ,
            ct);

        if ( !AiRoleHelper.IsTeacherAdminOrManager(roleCode) )
            throw new UnauthorizedAccessException("Only teacher/admin can generate AI editorial drafts.");

        var editorialContext = await _data.GetEditorialContextAsync(
            request.ProblemId ,
            ct);

        if ( editorialContext is null )
            throw new InvalidOperationException("Problem not found.");

        if ( string.IsNullOrWhiteSpace(editorialContext.DescriptionMd) )
            throw new InvalidOperationException("Cannot generate editorial because this problem has no statement.");

        var ctx = editorialContext.ToPromptContext();

        ctx["description_md"] = AiHash.Truncate(
            Convert.ToString(ctx["description_md"]) ,
            cfg.MaxProblemStatementChars);

        ctx["sample_testcases"] = AiHash.Truncate(
            Convert.ToString(ctx["sample_testcases"]) ,
            cfg.MaxSampleTestcasesChars);

        ctx["solution_signatures"] = AiHash.Truncate(
            Convert.ToString(ctx["solution_signatures"]) ,
            cfg.MaxTeacherSolutionChars);

        ctx["target_audience_code"] = targetAudienceCode;

        var contextHash = AiHash.Create(new
        {
            feature = FeatureCode ,
            problemId = request.ProblemId ,
            languageCode ,
            styleCode ,
            targetAudienceCode ,
            includePseudocode ,
            includeCorrectness ,
            includeComplexity ,
            promptVersion = cfg.PromptVersion ,
            responseMode = ResponseMode ,
            ctx
        });

        if ( cfg.UseCache )
        {
            var cached = await _data.GetCachedDraftAsync(
                request.ProblemId ,
                contextHash ,
                languageCode ,
                styleCode ,
                ct);

            if ( cached is not null )
            {
                object? cachedOutline = null;

                if ( !string.IsNullOrWhiteSpace(cached.ResponseJson) )
                {
                    try
                    {
                        using var cachedDoc = JsonDocument.Parse(cached.ResponseJson);

                        if ( cachedDoc.RootElement.TryGetProperty("outline" , out var cachedOutlineEl) )
                            cachedOutline = JsonSerializer.Deserialize<object>(cachedOutlineEl.GetRawText());
                    }
                    catch
                    {
                        cachedOutline = null;
                    }
                }

                return new AiEditorialDraftResponseDto(
                    DraftId: cached.DraftId ,
                    ProblemId: cached.ProblemId ,
                    DraftStatusCode: cached.DraftStatusCode ,
                    LanguageCode: cached.LanguageCode ,
                    StyleCode: cached.StyleCode ,
                    Title: cached.Title ,
                    ContentMd: cached.ContentMd ,
                    Outline: cachedOutline ,
                    Warnings: AiJsonReader.ReadStringArrayFromJson(cached.WarningsJson) ,
                    CreatedAt: cached.CreatedAt);
            }
        }

        var quota = AiRoleHelper.GetEditorialDailyQuota(
            roleCode ,
            cfg.DailyQuotaPerTeacher ,
            cfg.DailyQuotaPerAdmin);

        var usedToday = await _data.CountTodayRequestsAsync(
            request.CurrentUserId ,
            FeatureCode ,
            ct);

        if ( usedToday >= quota )
            throw new InvalidOperationException("You have reached today’s AI editorial generation limit.");

        var userPrompt = AiPromptFactory.BuildEditorialUserPrompt(
            languageCode ,
            styleCode ,
            targetAudienceCode ,
            includePseudocode ,
            includeCorrectness ,
            includeComplexity ,
            ctx);

        AiModelResult modelResult;
        Guid requestLogId;

        try
        {
            modelResult = await _ai.GenerateTextAsync(
                cfg.Model ,
                AiPromptFactory.BuildEditorialMarkdownSystemPrompt(cfg) ,
                userPrompt ,
                new AiGenerationSettings(
                    cfg.Temperature ,
                    cfg.TopP ,
                    cfg.MaxOutputTokens ,
                    cfg.TimeoutSeconds ,
                    "text/plain") ,
                ct);

            requestLogId = await _data.InsertRequestLogAsync(
                new AiRequestLogCreateDto(
                    UserId: request.CurrentUserId ,
                    FeatureCode: FeatureCode ,
                    ProviderCode: _options.Provider ,
                    ModelName: cfg.Model ,
                    PromptVersion: cfg.PromptVersion ,
                    RequestHash: contextHash ,
                    StatusCode: "success" ,
                    LanguageCode: languageCode ,
                    PromptTokens: modelResult.PromptTokens ,
                    CompletionTokens: modelResult.CompletionTokens ,
                    TotalTokens: modelResult.TotalTokens ,
                    LatencyMs: modelResult.LatencyMs ,
                    ErrorCode: null ,
                    ErrorMessage: null) ,
                ct);
        }
        catch ( Exception ex )
        {
            var errorCode = GetProviderErrorCode(ex);

            await InsertFailedRequestLogSafeAsync(
                request.CurrentUserId ,
                cfg.Model ,
                cfg.PromptVersion ,
                contextHash ,
                languageCode ,
                errorCode ,
                ex.Message ,
                ct);

            ThrowFriendlyProviderException(errorCode);

            throw;
        }

        var rawMarkdown = modelResult.JsonText ?? string.Empty;

        if ( !HasEndMarker(rawMarkdown) )
        {
            await InsertValidationFailedLogSafeAsync(
                request.CurrentUserId ,
                cfg.Model ,
                cfg.PromptVersion ,
                contextHash ,
                languageCode ,
                "draft_incomplete_missing_end_marker" ,
                "AI editorial draft appears incomplete because END_OF_EDITORIAL marker is missing." ,
                ct);

            throw new InvalidOperationException(
                "AI editorial draft appears incomplete. Please regenerate with higher MaxOutputTokens or shorter context.");
        }

        var contentMd = NormalizeEditorialMarkdown(
            rawMarkdown ,
            editorialContext.ProblemTitle);

        if ( LooksIncompleteMarkdown(contentMd , includePseudocode) )
        {
            await InsertValidationFailedLogSafeAsync(
                request.CurrentUserId ,
                cfg.Model ,
                cfg.PromptVersion ,
                contextHash ,
                languageCode ,
                "draft_incomplete_validation_failed" ,
                "AI editorial draft failed markdown completeness validation." ,
                ct);

            throw new InvalidOperationException(
                "AI editorial draft appears incomplete. Please regenerate with higher MaxOutputTokens or shorter context.");
        }

        var title = $"Editorial draft for {editorialContext.ProblemTitle}";
        var summaryMd = BuildSummaryFromMarkdown(contentMd);

        var confidence = 70;
        var confidenceLevelCode = "medium";

        var warnings = new[]
        {
            "This draft was generated by AI and may contain incorrect reasoning.",
            "Teacher review is required before publishing."
        };

        var assumptions = Array.Empty<string>();

        var outline = BuildOutlineFromMarkdown(contentMd);

        var warningsJson = JsonSerializer.Serialize(warnings);
        var assumptionsJson = JsonSerializer.Serialize(assumptions);

        var responseJson = JsonSerializer.Serialize(new
        {
            title ,
            summaryMd ,
            contentMd ,
            confidence ,
            confidenceLevelCode ,
            outline ,
            warnings ,
            assumptions ,
            responseMode = ResponseMode
        });

        var draftId = await _data.InsertEditorialDraftAsync(
            new AiEditorialDraftCreateDto(
                ProblemId: request.ProblemId ,
                RequestedByUserId: request.CurrentUserId ,
                AiRequestLogId: requestLogId ,
                ContextHash: contextHash ,
                LanguageCode: languageCode ,
                StyleCode: styleCode ,
                Title: title ,
                SummaryMd: summaryMd ,
                ContentMd: contentMd ,
                ConfidenceLevelCode: confidenceLevelCode ,
                ConfidenceScore: confidence ,
                WarningsJson: warningsJson ,
                AssumptionsJson: assumptionsJson ,
                ResponseJson: responseJson) ,
            ct);

        return new AiEditorialDraftResponseDto(
            DraftId: draftId ,
            ProblemId: request.ProblemId ,
            DraftStatusCode: "draft" ,
            LanguageCode: languageCode ,
            StyleCode: styleCode ,
            Title: title ,
            ContentMd: contentMd ,
            Outline: outline ,
            Warnings: warnings ,
            CreatedAt: DateTimeOffset.UtcNow);
    }

    private async Task InsertFailedRequestLogSafeAsync(
        Guid userId ,
        string modelName ,
        string promptVersion ,
        string contextHash ,
        string languageCode ,
        string errorCode ,
        string errorMessage ,
        CancellationToken ct)
    {
        try
        {
            await _data.InsertRequestLogAsync(
                new AiRequestLogCreateDto(
                    UserId: userId ,
                    FeatureCode: FeatureCode ,
                    ProviderCode: _options.Provider ,
                    ModelName: modelName ,
                    PromptVersion: promptVersion ,
                    RequestHash: contextHash ,
                    StatusCode: "failed" ,
                    LanguageCode: languageCode ,
                    PromptTokens: null ,
                    CompletionTokens: null ,
                    TotalTokens: null ,
                    LatencyMs: null ,
                    ErrorCode: errorCode ,
                    ErrorMessage: errorMessage) ,
                ct);
        }
        catch
        {
            // Do not hide the original provider/generation error because logging failed.
        }
    }

    private async Task InsertValidationFailedLogSafeAsync(
        Guid userId ,
        string modelName ,
        string promptVersion ,
        string contextHash ,
        string languageCode ,
        string errorCode ,
        string errorMessage ,
        CancellationToken ct)
    {
        try
        {
            await _data.InsertRequestLogAsync(
                new AiRequestLogCreateDto(
                    UserId: userId ,
                    FeatureCode: FeatureCode ,
                    ProviderCode: _options.Provider ,
                    ModelName: modelName ,
                    PromptVersion: promptVersion ,
                    RequestHash: contextHash ,
                    StatusCode: "failed" ,
                    LanguageCode: languageCode ,
                    PromptTokens: null ,
                    CompletionTokens: null ,
                    TotalTokens: null ,
                    LatencyMs: null ,
                    ErrorCode: errorCode ,
                    ErrorMessage: errorMessage) ,
                ct);
        }
        catch
        {
            // Validation error should still be returned even if logging fails.
        }
    }

    private static void ThrowFriendlyProviderException(string errorCode)
    {
        if ( errorCode == "provider_quota_exceeded" )
            throw new InvalidOperationException("AI provider quota has been exceeded. Please check billing or try another provider.");

        if ( errorCode == "provider_rate_limited" )
            throw new InvalidOperationException("AI provider rate limit has been reached. Please try again later.");

        if ( errorCode == "provider_unavailable" )
            throw new InvalidOperationException("AI provider is temporarily overloaded. Please try again later.");

        if ( errorCode == "provider_timeout" )
            throw new InvalidOperationException("AI provider timed out. Please try again later.");

        throw new InvalidOperationException("AI editorial generation is temporarily unavailable. Your problem data was not changed.");
    }

    private static string Normalize(string? value , string fallback)
        => string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();

    private static bool HasEndMarker(string raw)
    {
        return !string.IsNullOrWhiteSpace(raw)
               && raw.Contains(EndMarker , StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEditorialMarkdown(string raw , string? problemTitle)
    {
        raw = string.IsNullOrWhiteSpace(raw)
            ? "# AI Editorial Draft\n\nAI returned an empty draft."
            : raw.Trim();

        if ( raw.StartsWith("```markdown" , StringComparison.OrdinalIgnoreCase) )
            raw = raw["```markdown".Length..].Trim();

        if ( raw.StartsWith("```" , StringComparison.OrdinalIgnoreCase) )
            raw = raw["```".Length..].Trim();

        if ( raw.EndsWith("```" , StringComparison.OrdinalIgnoreCase) )
            raw = raw[..^3].Trim();

        raw = raw.Replace(EndMarker , "" , StringComparison.OrdinalIgnoreCase).Trim();

        if ( !raw.StartsWith("#") )
            raw = $"# Editorial: {problemTitle ?? "Problem"}\n\n" + raw;

        return raw;
    }

    private static string BuildSummaryFromMarkdown(string markdown)
    {
        var lines = markdown
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !x.StartsWith("#"))
            .Where(x => !x.StartsWith("```"))
            .Take(4)
            .ToArray();

        var summary = string.Join(" " , lines);

        if ( summary.Length > 700 )
            summary = summary[..700];

        return summary;
    }

    private static object BuildOutlineFromMarkdown(string markdown)
    {
        var sections = markdown
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.StartsWith("## "))
            .Select(x => x.TrimStart('#').Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();

        if ( sections.Length == 0 )
        {
            sections = new[]
            {
                "Problem Understanding",
                "Key Observation",
                "Algorithm",
                "Correctness Idea",
                "Complexity",
                "Edge Cases"
            };
        }

        return new
        {
            sections
        };
    }

    private static string GetProviderErrorCode(Exception ex)
    {
        if ( ex is TimeoutException )
            return "provider_timeout";

        if ( ex is OperationCanceledException )
            return "provider_cancelled";

        if ( ex.Message.Contains("insufficient_quota" , StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("RESOURCE_EXHAUSTED" , StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("quota" , StringComparison.OrdinalIgnoreCase) )
            return "provider_quota_exceeded";

        if ( ex.Message.Contains("429" , StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("rate_limit_exceeded" , StringComparison.OrdinalIgnoreCase) )
            return "provider_rate_limited";

        if ( ex.Message.Contains("503" , StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("UNAVAILABLE" , StringComparison.OrdinalIgnoreCase) )
            return "provider_unavailable";

        if ( ex.Message.Contains("model_not_found" , StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("does not exist" , StringComparison.OrdinalIgnoreCase) )
            return "provider_model_not_found";

        return "provider_error";
    }

    private static bool LooksIncompleteMarkdown(string markdown , bool includePseudocode)
    {
        if ( string.IsNullOrWhiteSpace(markdown) )
            return true;

        var trimmed = markdown.Trim();

        var requiredSections = includePseudocode
            ? new[]
            {
                "## 1. Problem Understanding",
                "## 2. Key Observation",
                "## 3. Algorithm",
                "## 4. Correctness Idea",
                "## 5. Complexity",
                "## 6. Edge Cases",
                "## 7. Pseudocode"
            }
            : new[]
            {
                "## 1. Problem Understanding",
                "## 2. Key Observation",
                "## 3. Algorithm",
                "## 4. Correctness Idea",
                "## 5. Complexity",
                "## 6. Edge Cases"
            };

        var minimumRequiredCount = includePseudocode ? 6 : 5;

        var sectionCount = requiredSections.Count(section =>
            trimmed.Contains(section , StringComparison.OrdinalIgnoreCase));

        var hasTooFewSections = sectionCount < minimumRequiredCount;

        var suspiciousEnding =
            trimmed.EndsWith("<=" , StringComparison.Ordinal)
            || trimmed.EndsWith(">=" , StringComparison.Ordinal)
            || trimmed.EndsWith("=" , StringComparison.Ordinal)
            || trimmed.EndsWith("`" , StringComparison.Ordinal)
            || trimmed.EndsWith("and" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("or" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("to" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("such that" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("where" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("because" , StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("," , StringComparison.Ordinal)
            || trimmed.EndsWith(":" , StringComparison.Ordinal)
            || trimmed.EndsWith("-" , StringComparison.Ordinal);

        var noFinalPunctuation =
            !trimmed.EndsWith("." , StringComparison.Ordinal)
            && !trimmed.EndsWith(")" , StringComparison.Ordinal)
            && !trimmed.EndsWith("]" , StringComparison.Ordinal)
            && !trimmed.EndsWith("```" , StringComparison.Ordinal);

        return hasTooFewSections || suspiciousEnding || noFinalPunctuation;
    }
}