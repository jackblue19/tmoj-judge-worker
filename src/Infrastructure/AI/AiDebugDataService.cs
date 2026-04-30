using Application.Abstractions.AI;
using Application.UseCases.AI.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.AI;

public sealed class AiDebugDataService : IAiDebugDataService
{
    private readonly TmojDbContext _db;

    public AiDebugDataService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<AiDebugContext?> GetDebugContextAsync(
        Guid submissionId ,
        Guid? resultId ,
        Guid currentUserId ,
        CancellationToken ct = default)
    {
        var sql =
    """
    WITH picked_results AS (
        SELECT
            res.id,
            res.submission_id,
            res.judge_run_id,
            res.status_code,
            res.input,
            res.expected_output,
            res.actual_output,
            res.checker_message,
            res.message,
            res.exit_code,
            res.signal,
            res.created_at
        FROM result res
        WHERE res.submission_id = CAST(@submission_id AS uuid)
          AND (
                CAST(@result_id AS uuid) IS NULL
                OR res.id = CAST(@result_id AS uuid)
              )
          AND (
                res.input IS NOT NULL
                OR res.expected_output IS NOT NULL
                OR res.actual_output IS NOT NULL
                OR res.checker_message IS NOT NULL
                OR res.message IS NOT NULL
              )
        ORDER BY
            CASE
                WHEN res.status_code IS NULL THEN 1
                WHEN lower(res.status_code) IN ('ac', 'accepted', 'correct') THEN 1
                ELSE 0
            END,
            res.created_at DESC
        LIMIT 3
    )
    SELECT
        s.id AS submission_id,
        s.user_id,
        s.problem_id,
        s.runtime_id,
        s.status_code AS submission_status_code,
        s.verdict_code,
        s.source_code,
        s.custom_input,

        p.title AS problem_title,
        p.description_md,
        p.problem_mode,
        p.time_limit_ms,
        p.memory_limit_kb,

        rt.runtime_name,
        rt.runtime_version,

        (
            SELECT pr.id
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS result_id,

        (
            SELECT pr.judge_run_id
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS judge_run_id,

        (
            SELECT pr.status_code
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS result_status_code,

        (
            SELECT pr.input
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS input,

        (
            SELECT pr.expected_output
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS expected_output,

        (
            SELECT pr.actual_output
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS actual_output,

        (
            SELECT pr.checker_message
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS checker_message,

        (
            SELECT pr.message
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS message,

        (
            SELECT pr.exit_code
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS exit_code,

        (
            SELECT pr.signal
            FROM picked_results pr
            ORDER BY pr.created_at DESC
            LIMIT 1
        ) AS signal,

        (
            SELECT string_agg(
                'Testcase Result #' || row_number_text || E'\n'
                || 'Status: ' || COALESCE(status_code, '') || E'\n'
                || 'Input:\n' || COALESCE(input, '') || E'\n'
                || 'Expected Output:\n' || COALESCE(expected_output, '') || E'\n'
                || 'Actual Output:\n' || COALESCE(actual_output, '') || E'\n'
                || 'Checker Message:\n' || COALESCE(checker_message, '') || E'\n'
                || 'Message:\n' || COALESCE(message, ''),
                E'\n\n---\n\n'
            )
            FROM (
                SELECT
                    pr.*,
                    row_number() OVER (ORDER BY pr.created_at DESC)::text AS row_number_text
                FROM picked_results pr
            ) x
        ) AS visible_results_text,

        COALESCE(role_current.role_code, 'student') AS current_user_role_code
    FROM submissions s
    JOIN problems p ON p.id = s.problem_id
    LEFT JOIN runtime rt ON rt.id = s.runtime_id
    LEFT JOIN users u_current ON u_current.user_id = CAST(@current_user_id AS uuid)
    LEFT JOIN role role_current ON role_current.role_id = u_current.role_id
    WHERE s.id = CAST(@submission_id AS uuid)
      AND s.is_deleted = false
      AND (
            s.user_id = CAST(@current_user_id AS uuid)
            OR COALESCE(role_current.role_code, 'student') IN ('admin', 'teacher', 'manager')
          )
    LIMIT 1;
    """;

        var row = await QuerySingleAsync(sql , new Dictionary<string , object?>
        {
            ["submission_id"] = submissionId ,
            ["result_id"] = resultId ,
            ["current_user_id"] = currentUserId
        } , ct);

        if ( row is null )
            return null;

        return new AiDebugContext
        {
            SubmissionId = GetGuid(row , "submission_id")!.Value ,
            UserId = GetGuid(row , "user_id")!.Value ,
            ProblemId = GetGuid(row , "problem_id")!.Value ,
            RuntimeId = GetGuid(row , "runtime_id") ,
            ResultId = GetGuid(row , "result_id") ,
            JudgeRunId = GetGuid(row , "judge_run_id") ,

            CurrentUserRoleCode = GetString(row , "current_user_role_code") ,
            SubmissionStatusCode = GetString(row , "submission_status_code") ,
            VerdictCode = GetString(row , "verdict_code") ,
            ResultStatusCode = GetString(row , "result_status_code") ,

            SourceCode = GetString(row , "source_code") ,
            CustomInput = GetString(row , "custom_input") ,

            ProblemTitle = GetString(row , "problem_title") ,
            DescriptionMd = GetString(row , "description_md") ,
            ProblemMode = GetString(row , "problem_mode") ,
            TimeLimitMs = GetInt(row , "time_limit_ms") ,
            MemoryLimitKb = GetInt(row , "memory_limit_kb") ,

            RuntimeName = GetString(row , "runtime_name") ,
            RuntimeVersion = GetString(row , "runtime_version") ,

            Input = GetString(row , "input") ,
            ExpectedOutput = GetString(row , "expected_output") ,
            ActualOutput = GetString(row , "actual_output") ,
            CheckerMessage = GetString(row , "checker_message") ,
            Message = GetString(row , "message") ,
            ExitCode = GetInt(row , "exit_code") ,
            Signal = GetInt(row , "signal") ,

            VisibleResultsText = GetString(row , "visible_results_text")
        };
    }

    public async Task<AiDebugCachedResult?> GetCachedDebugAsync(
        Guid submissionId ,
        Guid? resultId ,
        string contextHash ,
        CancellationToken ct = default)
    {
        var sql =
            """
            SELECT id, verdict_code, result_status_code, summary_md, suspected_issue_code,
                   confidence_score, response_json, created_at
            FROM ai_debug_sessions
            WHERE submission_id = @submission_id
              AND COALESCE(result_id, '00000000-0000-0000-0000-000000000000'::uuid)
                  = COALESCE(CAST(@result_id AS uuid), '00000000-0000-0000-0000-000000000000'::uuid)
              AND context_hash = @context_hash
              AND is_visible_to_user = true
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        var row = await QuerySingleAsync(sql , new Dictionary<string , object?>
        {
            ["submission_id"] = submissionId ,
            ["result_id"] = resultId ,
            ["context_hash"] = contextHash
        } , ct);

        if ( row is null )
            return null;

        return new AiDebugCachedResult(
            DebugSessionId: GetGuid(row , "id")!.Value ,
            VerdictCode: GetString(row , "verdict_code") ,
            ResultStatusCode: GetString(row , "result_status_code") ,
            Summary: GetString(row , "summary_md") ?? "" ,
            SuspectedIssueCode: GetString(row , "suspected_issue_code") ?? "unknown" ,
            Confidence: Convert.ToInt32(row["confidence_score"] ?? 50) ,
            ResponseJson: Convert.ToString(row["response_json"]) ?? "{}" ,
            CreatedAt: ToDateTimeOffset(row["created_at"]));
    }

    public async Task<int> CountTodayRequestsAsync(
        Guid userId ,
        string featureCode ,
        CancellationToken ct = default)
    {
        var sql =
            """
            SELECT COUNT(*)
            FROM ai_request_logs
            WHERE requested_by_user_id = @user_id
              AND feature_code = @feature_code
              AND status_code IN ('success', 'cached')
              AND created_at >= date_trunc('day', now());
            """;

        return await ExecuteScalarIntAsync(sql , new Dictionary<string , object?>
        {
            ["user_id"] = userId ,
            ["feature_code"] = featureCode
        } , ct);
    }

    public Task<Guid> InsertRequestLogAsync(
        AiRequestLogCreateDto dto ,
        CancellationToken ct = default)
    {
        var sql =
            """
            INSERT INTO ai_request_logs (
                requested_by_user_id, feature_code, provider_code, model_name,
                prompt_version, request_hash, status_code, language_code,
                prompt_tokens, completion_tokens, total_tokens, latency_ms,
                error_code, error_message
            )
            VALUES (
                @user_id, @feature_code, @provider_code, @model_name,
                @prompt_version, @request_hash, @status_code, @language_code,
                @prompt_tokens, @completion_tokens, @total_tokens, @latency_ms,
                @error_code, @error_message
            )
            RETURNING id;
            """;

        return ExecuteScalarGuidAsync(sql , new Dictionary<string , object?>
        {
            ["user_id"] = dto.UserId ,
            ["feature_code"] = dto.FeatureCode ,
            ["provider_code"] = dto.ProviderCode ,
            ["model_name"] = dto.ModelName ,
            ["prompt_version"] = dto.PromptVersion ,
            ["request_hash"] = dto.RequestHash ,
            ["status_code"] = dto.StatusCode ,
            ["language_code"] = dto.LanguageCode ,
            ["prompt_tokens"] = dto.PromptTokens ,
            ["completion_tokens"] = dto.CompletionTokens ,
            ["total_tokens"] = dto.TotalTokens ,
            ["latency_ms"] = dto.LatencyMs ,
            ["error_code"] = dto.ErrorCode ,
            ["error_message"] = dto.ErrorMessage
        } , ct);
    }

    public Task<Guid> InsertDebugSessionAsync(
        AiDebugSessionCreateDto dto ,
        CancellationToken ct = default)
    {
        var sql =
            """
        INSERT INTO ai_debug_sessions (
            user_id,
            problem_id,
            submission_id,
            result_id,
            judge_run_id,
            runtime_id,
            ai_request_log_id,
            context_hash,
            verdict_code,
            submission_status_code,
            result_status_code,
            suspected_issue_code,
            confidence_level_code,
            confidence_score,
            summary_md,
            response_json,
            response_status_code,
            is_visible_to_user,
            created_at,
            updated_at
        )
        VALUES (
            @user_id,
            @problem_id,
            @submission_id,
            @result_id,
            @judge_run_id,
            @runtime_id,
            @ai_request_log_id,
            @context_hash,
            @verdict_code,
            @submission_status_code,
            @result_status_code,
            @suspected_issue_code,
            @confidence_level_code,
            @confidence_score,
            @summary_md,
            CAST(@response_json AS jsonb),
            'generated',
            true,
            now(),
            now()
        )
        ON CONFLICT (
            submission_id,
            COALESCE(result_id, '00000000-0000-0000-0000-000000000000'::uuid),
            context_hash
        )
        DO UPDATE SET
            user_id = EXCLUDED.user_id,
            problem_id = EXCLUDED.problem_id,
            judge_run_id = EXCLUDED.judge_run_id,
            runtime_id = EXCLUDED.runtime_id,
            ai_request_log_id = EXCLUDED.ai_request_log_id,
            verdict_code = EXCLUDED.verdict_code,
            submission_status_code = EXCLUDED.submission_status_code,
            result_status_code = EXCLUDED.result_status_code,
            suspected_issue_code = EXCLUDED.suspected_issue_code,
            confidence_level_code = EXCLUDED.confidence_level_code,
            confidence_score = EXCLUDED.confidence_score,
            summary_md = EXCLUDED.summary_md,
            response_json = EXCLUDED.response_json,
            response_status_code = 'generated',
            is_visible_to_user = true,
            updated_at = now()
        RETURNING id;
        """;

        return ExecuteScalarGuidAsync(sql , new Dictionary<string , object?>
        {
            ["user_id"] = dto.UserId ,
            ["problem_id"] = dto.ProblemId ,
            ["submission_id"] = dto.SubmissionId ,
            ["result_id"] = dto.ResultId ,
            ["judge_run_id"] = dto.JudgeRunId ,
            ["runtime_id"] = dto.RuntimeId ,
            ["ai_request_log_id"] = dto.AiRequestLogId ,
            ["context_hash"] = dto.ContextHash ,
            ["verdict_code"] = dto.VerdictCode ,
            ["submission_status_code"] = dto.SubmissionStatusCode ,
            ["result_status_code"] = dto.ResultStatusCode ,
            ["suspected_issue_code"] = dto.SuspectedIssueCode ,
            ["confidence_level_code"] = dto.ConfidenceLevelCode ,
            ["confidence_score"] = dto.ConfidenceScore ,
            ["summary_md"] = dto.SummaryMd ,
            ["response_json"] = dto.ResponseJson
        } , ct);
    }

    private async Task<Dictionary<string , object?>?> QuerySingleAsync(
        string sql ,
        Dictionary<string , object?> parameters ,
        CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();

        if ( conn.State != ConnectionState.Open )
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        foreach ( var p in parameters )
        {
            var param = cmd.CreateParameter();
            param.ParameterName = p.Key;
            param.Value = p.Value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if ( !await reader.ReadAsync(ct) )
            return null;

        var row = new Dictionary<string , object?>();

        for ( var i = 0; i < reader.FieldCount; i++ )
            row[reader.GetName(i)] = await reader.IsDBNullAsync(i , ct) ? null : reader.GetValue(i);

        return row;
    }

    private async Task<object?> ExecuteScalarAsync(
        string sql ,
        Dictionary<string , object?> parameters ,
        CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();

        if ( conn.State != ConnectionState.Open )
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        foreach ( var p in parameters )
        {
            var param = cmd.CreateParameter();
            param.ParameterName = p.Key;
            param.Value = p.Value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        return await cmd.ExecuteScalarAsync(ct);
    }

    private async Task<int> ExecuteScalarIntAsync(
        string sql ,
        Dictionary<string , object?> parameters ,
        CancellationToken ct)
    {
        var value = await ExecuteScalarAsync(sql , parameters , ct);
        return Convert.ToInt32(value);
    }

    private async Task<Guid> ExecuteScalarGuidAsync(
        string sql ,
        Dictionary<string , object?> parameters ,
        CancellationToken ct)
    {
        var value = await ExecuteScalarAsync(sql , parameters , ct);
        return (Guid) value!;
    }

    private static string? GetString(Dictionary<string , object?> row , string key)
        => row.TryGetValue(key , out var value) ? Convert.ToString(value) : null;

    private static Guid? GetGuid(Dictionary<string , object?> row , string key)
    {
        if ( !row.TryGetValue(key , out var value) || value is null )
            return null;

        return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value)!);
    }

    private static int? GetInt(Dictionary<string , object?> row , string key)
    {
        if ( !row.TryGetValue(key , out var value) || value is null )
            return null;

        return Convert.ToInt32(value);
    }

    private static DateTimeOffset ToDateTimeOffset(object? value)
    {
        if ( value is DateTimeOffset dto )
            return dto;

        if ( value is DateTime dt )
            return new DateTimeOffset(DateTime.SpecifyKind(dt , DateTimeKind.Utc));

        return DateTimeOffset.UtcNow;
    }
}