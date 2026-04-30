using System.Text.Json;
using Application.Abstractions.AI;
using Application.Common.AI;
using Application.UseCases.AI.Dtos;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.UseCases.AI;

public sealed record GenerateAiDebugCommand(
    Guid SubmissionId ,
    Guid? ResultId ,
    Guid CurrentUserId ,
    string? LanguageCode) : IRequest<AiDebugResponseDto>;

public sealed class GenerateAiDebugCommandHandler
    : IRequestHandler<GenerateAiDebugCommand , AiDebugResponseDto>
{
    private readonly IAiModelClient _ai;
    private readonly IAiDebugDataService _data;
    private readonly AiOptions _options;

    public GenerateAiDebugCommandHandler(
        IAiModelClient ai ,
        IAiDebugDataService data ,
        IOptions<AiOptions> options)
    {
        _ai = ai;
        _data = data;
        _options = options.Value;
    }

    public async Task<AiDebugResponseDto> Handle(
        GenerateAiDebugCommand request ,
        CancellationToken ct)
    {
        var cfg = _options.Debug;
        var languageCode = Normalize(request.LanguageCode , cfg.LanguageCode);

        var debugContext = await _data.GetDebugContextAsync(
            request.SubmissionId ,
            request.ResultId ,
            request.CurrentUserId ,
            ct);

        if ( debugContext is null )
            throw new InvalidOperationException("Submission not found or permission denied.");

        if ( debugContext.SubmissionStatusCode is not ("done" or "failed") )
            throw new InvalidOperationException("AI Debug is only available after judging is finished.");

        if ( string.Equals(debugContext.VerdictCode , "ac" , StringComparison.OrdinalIgnoreCase) )
            throw new InvalidOperationException("AI Debug is not available for accepted submissions.");

        var ctx = debugContext.ToPromptContext();

        ctx["description_md"] = AiHash.Truncate(
            Convert.ToString(ctx["description_md"]) ,
            cfg.MaxProblemStatementChars);

        ctx["source_code"] = AiHash.Truncate(
            Convert.ToString(ctx["source_code"]) ,
            cfg.MaxSourceCodeChars);

        ctx["input"] = AiHash.Truncate(
            Convert.ToString(ctx["input"]) ,
            cfg.MaxTestcaseChars);

        ctx["expected_output"] = AiHash.Truncate(
            Convert.ToString(ctx["expected_output"]) ,
            cfg.MaxTestcaseChars);

        ctx["actual_output"] = AiHash.Truncate(
            Convert.ToString(ctx["actual_output"]) ,
            cfg.MaxTestcaseChars);

        ctx["checker_message"] = AiHash.Truncate(
            Convert.ToString(ctx["checker_message"]) ,
            cfg.MaxMessageChars);

        ctx["message"] = AiHash.Truncate(
            Convert.ToString(ctx["message"]) ,
            cfg.MaxMessageChars);

        ctx["visible_results_text"] = AiHash.Truncate(
            Convert.ToString(ctx["visible_results_text"]) ,
            cfg.MaxTestcaseChars * 3);

        var hasVisibleEvidence =
            !string.IsNullOrWhiteSpace(Convert.ToString(ctx["visible_results_text"]))
            || !string.IsNullOrWhiteSpace(Convert.ToString(ctx["input"]))
            || !string.IsNullOrWhiteSpace(Convert.ToString(ctx["expected_output"]))
            || !string.IsNullOrWhiteSpace(Convert.ToString(ctx["actual_output"]))
            || !string.IsNullOrWhiteSpace(Convert.ToString(ctx["checker_message"]))
            || !string.IsNullOrWhiteSpace(Convert.ToString(ctx["message"]));

        if ( !hasVisibleEvidence )
            throw new InvalidOperationException("AI Debug is not available because no visible testcase details can be used.");

        var contextHash = AiHash.Create(new
        {
            feature = "debug_assistant" ,
            submissionId = request.SubmissionId ,
            resultId = request.ResultId ,
            languageCode ,
            promptVersion = cfg.PromptVersion ,
            allowFullSolution = cfg.AllowFullSolution ,
            allowTeacherSolutionForStudent = cfg.AllowTeacherSolutionForStudent ,
            ctx
        });

        if ( cfg.UseCache )
        {
            var cached = await _data.GetCachedDebugAsync(
                request.SubmissionId ,
                request.ResultId ,
                contextHash ,
                ct);

            if ( cached is not null )
            {
                var cachedJson = AiModelJsonNormalizer.NormalizeOrFallback(
                    cached.ResponseJson ,
                    "debug_assistant" ,
                    "AI Debug Explanation" ,
                    "Cached AI debug response was not valid JSON.");

                using var cachedDoc = JsonDocument.Parse(cachedJson);
                var cachedRoot = cachedDoc.RootElement;

                return new AiDebugResponseDto(
                    DebugSessionId: cached.DebugSessionId ,
                    Source: "cached" ,
                    VerdictCode: cached.VerdictCode ,
                    ResultStatusCode: cached.ResultStatusCode ,
                    Summary: cached.Summary ,
                    SuspectedIssueCode: cached.SuspectedIssueCode ,
                    Confidence: cached.Confidence ,
                    Sections: AiJsonReader.ReadDebugSections(cachedRoot) ,
                    SafetyNote: AiJsonReader.ReadString(
                        cachedRoot ,
                        "safetyNote" ,
                        "AI có thể giải thích sai. Hãy dùng như gợi ý hỗ trợ, không thay thế kết quả judge.") ,
                    CreatedAt: cached.CreatedAt);
            }
        }

        var quota = AiRoleHelper.GetDebugDailyQuota(
            debugContext.CurrentUserRoleCode ,
            cfg.DailyQuotaPerStudent ,
            cfg.DailyQuotaPerTeacher ,
            cfg.DailyQuotaPerAdmin);

        var usedToday = await _data.CountTodayRequestsAsync(
            request.CurrentUserId ,
            "debug_assistant" ,
            ct);

        if ( usedToday >= quota )
            throw new InvalidOperationException("You have reached today’s AI debug limit.");

        var userPrompt = AiPromptFactory.BuildDebugUserPrompt(languageCode , ctx);

        AiModelResult modelResult;
        Guid requestLogId;

        try
        {
            modelResult = await _ai.GenerateJsonAsync(
                cfg.Model ,
                AiPromptFactory.BuildDebugSystemPrompt(cfg) ,
                userPrompt ,
                AiJsonSchemas.DebugResponseSchema ,
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
                    FeatureCode: "debug_assistant" ,
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
                    FeatureCode: "debug_assistant" ,
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

            throw new InvalidOperationException("AI Debug is temporarily unavailable. Your submission result is still valid.");
        }

        var normalizedJson = AiModelJsonNormalizer.NormalizeOrFallback(
            modelResult.JsonText ,
            "debug_assistant" ,
            "AI Debug Explanation" ,
            "AI generated a debug explanation, but the response was not valid JSON.");

        using var doc = JsonDocument.Parse(normalizedJson);
        var root = doc.RootElement;

        var summary = AiJsonReader.ReadString(
            root ,
            "summary" ,
            "Không có tóm tắt.");

        var suspectedIssueCode = AiJsonReader.ReadString(
            root ,
            "suspectedIssueCode" ,
            "unknown");

        var confidence = AiJsonReader.ReadInt(
            root ,
            "confidence" ,
            50);

        var confidenceLevelCode = AiJsonReader.ReadString(
            root ,
            "confidenceLevelCode" ,
            "medium");

        var safetyNote = AiJsonReader.ReadString(
            root ,
            "safetyNote" ,
            "AI có thể giải thích sai. Hãy dùng như gợi ý hỗ trợ, không thay thế kết quả judge.");

        var debugSessionId = await _data.InsertDebugSessionAsync(
            new AiDebugSessionCreateDto(
                UserId: request.CurrentUserId ,
                ProblemId: debugContext.ProblemId ,
                SubmissionId: debugContext.SubmissionId ,
                ResultId: debugContext.ResultId ,
                JudgeRunId: debugContext.JudgeRunId ,
                RuntimeId: debugContext.RuntimeId ,
                AiRequestLogId: requestLogId ,
                ContextHash: contextHash ,
                VerdictCode: debugContext.VerdictCode ,
                SubmissionStatusCode: debugContext.SubmissionStatusCode ,
                ResultStatusCode: debugContext.ResultStatusCode ,
                SuspectedIssueCode: suspectedIssueCode ,
                ConfidenceLevelCode: confidenceLevelCode ,
                ConfidenceScore: confidence ,
                SummaryMd: summary ,
                ResponseJson: normalizedJson) ,
            ct);

        return new AiDebugResponseDto(
            DebugSessionId: debugSessionId ,
            Source: "generated" ,
            VerdictCode: debugContext.VerdictCode ,
            ResultStatusCode: debugContext.ResultStatusCode ,
            Summary: summary ,
            SuspectedIssueCode: suspectedIssueCode ,
            Confidence: confidence ,
            Sections: AiJsonReader.ReadDebugSections(root) ,
            SafetyNote: safetyNote ,
            CreatedAt: DateTimeOffset.UtcNow);
    }

    private static string Normalize(string? value , string fallback)
        => string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();
}