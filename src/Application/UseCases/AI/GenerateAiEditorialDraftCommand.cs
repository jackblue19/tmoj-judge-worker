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
            feature = "editorial_draft" ,
            problemId = request.ProblemId ,
            languageCode ,
            styleCode ,
            targetAudienceCode ,
            includePseudocode ,
            includeCorrectness ,
            includeComplexity ,
            promptVersion = cfg.PromptVersion ,
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
                    var cachedJson = AiModelJsonNormalizer.NormalizeOrFallback(
                        cached.ResponseJson ,
                        "editorial_draft" ,
                        cached.Title ,
                        "Cached AI editorial draft was not valid JSON.");

                    using var cachedDoc = JsonDocument.Parse(cachedJson);

                    if ( cachedDoc.RootElement.TryGetProperty("outline" , out var cachedOutlineEl) )
                        cachedOutline = JsonSerializer.Deserialize<object>(cachedOutlineEl.GetRawText());
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
            "editorial_draft" ,
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
            modelResult = await _ai.GenerateJsonAsync(
                cfg.Model ,
                AiPromptFactory.BuildEditorialSystemPrompt(cfg) ,
                userPrompt ,
                AiJsonSchemas.EditorialResponseSchema ,
                new AiGenerationSettings(
                    cfg.Temperature ,
                    cfg.TopP ,
                    cfg.MaxOutputTokens ,
                    cfg.TimeoutSeconds ,
                    cfg.ResponseMimeType) ,
                ct);

            requestLogId = await _data.InsertRequestLogAsync(
                new AiRequestLogCreateDto(
                    UserId: request.CurrentUserId ,
                    FeatureCode: "editorial_draft" ,
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
            await _data.InsertRequestLogAsync(
                new AiRequestLogCreateDto(
                    UserId: request.CurrentUserId ,
                    FeatureCode: "editorial_draft" ,
                    ProviderCode: _options.Provider ,
                    ModelName: cfg.Model ,
                    PromptVersion: cfg.PromptVersion ,
                    RequestHash: contextHash ,
                    StatusCode: "failed" ,
                    LanguageCode: languageCode ,
                    PromptTokens: null ,
                    CompletionTokens: null ,
                    TotalTokens: null ,
                    LatencyMs: null ,
                    ErrorCode: "provider_error" ,
                    ErrorMessage: ex.Message) ,
                ct);

            throw new InvalidOperationException("AI editorial generation is temporarily unavailable. Your problem data was not changed.");
        }

        var normalizedJson = AiModelJsonNormalizer.NormalizeOrFallback(
            modelResult.JsonText ,
            "editorial_draft" ,
            $"Editorial draft for {editorialContext.ProblemTitle}" ,
            "AI generated an editorial draft, but the response was not valid JSON.");

        using var doc = JsonDocument.Parse(normalizedJson);
        var root = doc.RootElement;

        var title = AiJsonReader.ReadString(
            root ,
            "title" ,
            $"Editorial draft for {editorialContext.ProblemTitle}");

        var summaryMd = AiJsonReader.ReadString(
            root ,
            "summaryMd" ,
            "");

        var contentMd = AiJsonReader.ReadString(
            root ,
            "contentMd" ,
            "");

        if ( string.IsNullOrWhiteSpace(contentMd) )
            contentMd = "AI generated an empty editorial draft. Please regenerate or write manually.";

        var confidence = AiJsonReader.ReadInt(
            root ,
            "confidence" ,
            50);

        var confidenceLevelCode = AiJsonReader.ReadString(
            root ,
            "confidenceLevelCode" ,
            "medium");

        var warningsJson = root.TryGetProperty("warnings" , out var warningsEl)
            ? warningsEl.GetRawText()
            : "[]";

        var assumptionsJson = root.TryGetProperty("assumptions" , out var assumptionsEl)
            ? assumptionsEl.GetRawText()
            : "[]";

        var outline = root.TryGetProperty("outline" , out var outlineEl)
            ? JsonSerializer.Deserialize<object>(outlineEl.GetRawText())
            : null;

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
                ResponseJson: normalizedJson) ,
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
            Warnings: AiJsonReader.ReadStringArray(root , "warnings") ,
            CreatedAt: DateTimeOffset.UtcNow);
    }

    private static string Normalize(string? value , string fallback)
        => string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();
}