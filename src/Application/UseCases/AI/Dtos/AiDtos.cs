using Application.Abstractions.AI;

namespace Application.UseCases.AI.Dtos;

public sealed record GenerateAiDebugRequestDto(
    Guid? ResultId ,
    string? LanguageCode);

public sealed record AiDebugSectionDto(
    string Title ,
    string ContentMd);

public sealed record AiDebugResponseDto(
    Guid DebugSessionId ,
    string Source ,
    string? VerdictCode ,
    string? ResultStatusCode ,
    string Summary ,
    string SuspectedIssueCode ,
    int Confidence ,
    IReadOnlyList<AiDebugSectionDto> Sections ,
    string SafetyNote ,
    DateTimeOffset CreatedAt);

public sealed record GenerateAiEditorialDraftRequestDto(
    string? LanguageCode ,
    string? StyleCode ,
    string? TargetAudienceCode ,
    bool? IncludePseudocode ,
    bool? IncludeCorrectness ,
    bool? IncludeComplexity);

public sealed record AiEditorialDraftResponseDto(
    Guid DraftId ,
    Guid ProblemId ,
    string DraftStatusCode ,
    string LanguageCode ,
    string StyleCode ,
    string Title ,
    string ContentMd ,
    object? Outline ,
    IReadOnlyList<string> Warnings ,
    DateTimeOffset CreatedAt);

public sealed record AiDebugContext
{
    public Guid SubmissionId { get; init; }
    public Guid UserId { get; init; }
    public Guid ProblemId { get; init; }
    public Guid? RuntimeId { get; init; }
    public Guid? ResultId { get; init; }
    public Guid? JudgeRunId { get; init; }

    public string? CurrentUserRoleCode { get; init; }
    public string? SubmissionStatusCode { get; init; }
    public string? VerdictCode { get; init; }
    public string? ResultStatusCode { get; init; }

    public string? SourceCode { get; init; }
    public string? CustomInput { get; init; }

    public string? ProblemTitle { get; init; }
    public string? DescriptionMd { get; init; }
    public string? ProblemMode { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }

    public string? RuntimeName { get; init; }
    public string? RuntimeVersion { get; init; }

    public string? Input { get; init; }
    public string? ExpectedOutput { get; init; }
    public string? ActualOutput { get; init; }
    public string? CheckerMessage { get; init; }
    public string? Message { get; init; }
    public int? ExitCode { get; init; }
    public int? Signal { get; init; }

    public string? VisibleResultsText { get; init; }

    public Dictionary<string , object?> ToPromptContext()
    {
        return new Dictionary<string , object?>
        {
            ["submission_id"] = SubmissionId ,
            ["problem_id"] = ProblemId ,
            ["runtime_id"] = RuntimeId ,
            ["result_id"] = ResultId ,
            ["judge_run_id"] = JudgeRunId ,
            ["current_user_role_code"] = CurrentUserRoleCode ,
            ["submission_status_code"] = SubmissionStatusCode ,
            ["verdict_code"] = VerdictCode ,
            ["result_status_code"] = ResultStatusCode ,
            ["source_code"] = SourceCode ,
            ["custom_input"] = CustomInput ,
            ["problem_title"] = ProblemTitle ,
            ["description_md"] = DescriptionMd ,
            ["problem_mode"] = ProblemMode ,
            ["time_limit_ms"] = TimeLimitMs ,
            ["memory_limit_kb"] = MemoryLimitKb ,
            ["runtime_name"] = RuntimeName ,
            ["runtime_version"] = RuntimeVersion ,
            ["input"] = Input ,
            ["expected_output"] = ExpectedOutput ,
            ["actual_output"] = ActualOutput ,
            ["checker_message"] = CheckerMessage ,
            ["message"] = Message ,
            ["exit_code"] = ExitCode ,
            ["signal"] = Signal,
            ["visible_results_text"] = VisibleResultsText
        };
    }
}

public sealed record AiDebugCachedResult(
    Guid DebugSessionId ,
    string? VerdictCode ,
    string? ResultStatusCode ,
    string Summary ,
    string SuspectedIssueCode ,
    int Confidence ,
    string ResponseJson ,
    DateTimeOffset CreatedAt);

public sealed record AiEditorialContext
{
    public Guid ProblemId { get; init; }
    public string? ProblemTitle { get; init; }
    public string? DescriptionMd { get; init; }
    public string? Difficulty { get; init; }
    public string? ProblemMode { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitKb { get; init; }
    public string? StatusCode { get; init; }
    public string? VisibilityCode { get; init; }
    public string? Tags { get; init; }
    public string? SampleTestcases { get; init; }
    public string? SolutionSignatures { get; init; }

    public Dictionary<string , object?> ToPromptContext()
    {
        return new Dictionary<string , object?>
        {
            ["problem_id"] = ProblemId ,
            ["problem_title"] = ProblemTitle ,
            ["description_md"] = DescriptionMd ,
            ["difficulty"] = Difficulty ,
            ["problem_mode"] = ProblemMode ,
            ["time_limit_ms"] = TimeLimitMs ,
            ["memory_limit_kb"] = MemoryLimitKb ,
            ["status_code"] = StatusCode ,
            ["visibility_code"] = VisibilityCode ,
            ["tags"] = Tags ,
            ["sample_testcases"] = SampleTestcases ,
            ["solution_signatures"] = SolutionSignatures
        };
    }
}

public sealed record AiEditorialCachedDraft(
    Guid DraftId ,
    Guid ProblemId ,
    string DraftStatusCode ,
    string LanguageCode ,
    string StyleCode ,
    string Title ,
    string ContentMd ,
    string? ResponseJson ,
    string? WarningsJson ,
    DateTimeOffset CreatedAt);

public sealed record AiRequestLogCreateDto(
    Guid UserId ,
    string FeatureCode ,
    string ProviderCode ,
    string ModelName ,
    string PromptVersion ,
    string RequestHash ,
    string StatusCode ,
    string LanguageCode ,
    int? PromptTokens ,
    int? CompletionTokens ,
    int? TotalTokens ,
    int? LatencyMs ,
    string? ErrorCode ,
    string? ErrorMessage);

public sealed record AiDebugSessionCreateDto(
    Guid UserId ,
    Guid ProblemId ,
    Guid SubmissionId ,
    Guid? ResultId ,
    Guid? JudgeRunId ,
    Guid? RuntimeId ,
    Guid AiRequestLogId ,
    string ContextHash ,
    string? VerdictCode ,
    string? SubmissionStatusCode ,
    string? ResultStatusCode ,
    string SuspectedIssueCode ,
    string ConfidenceLevelCode ,
    int ConfidenceScore ,
    string SummaryMd ,
    string ResponseJson);

public sealed record AiEditorialDraftCreateDto(
    Guid ProblemId ,
    Guid RequestedByUserId ,
    Guid AiRequestLogId ,
    string ContextHash ,
    string LanguageCode ,
    string StyleCode ,
    string Title ,
    string SummaryMd ,
    string ContentMd ,
    string ConfidenceLevelCode ,
    int ConfidenceScore ,
    string WarningsJson ,
    string AssumptionsJson ,
    string ResponseJson);