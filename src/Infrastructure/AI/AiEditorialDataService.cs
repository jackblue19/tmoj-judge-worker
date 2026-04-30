using Application.Abstractions.AI;
using Application.UseCases.AI.Dtos;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.AI;

public sealed class AiEditorialDataService : IAiEditorialDataService
{
    private readonly TmojDbContext _db;

    public AiEditorialDataService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetCurrentUserRoleCodeAsync(
        Guid userId ,
        CancellationToken ct = default)
    {
        var sql =
            """
            SELECT COALESCE(r.role_code, 'student')
            FROM users u
            LEFT JOIN role r ON r.role_id = u.role_id
            WHERE u.user_id = @user_id
            LIMIT 1;
            """;

        var value = await ExecuteScalarAsync(sql , new Dictionary<string , object?>
        {
            ["user_id"] = userId
        } , ct);

        return Convert.ToString(value) ?? "student";
    }

    public async Task<AiEditorialContext?> GetEditorialContextAsync(
        Guid problemId ,
        CancellationToken ct = default)
    {
        var sql =
            """
            SELECT
                p.id AS problem_id,
                p.title AS problem_title,
                p.description_md,
                p.difficulty,
                p.problem_mode,
                p.time_limit_ms,
                p.memory_limit_kb,
                p.status_code,
                p.visibility_code,
                COALESCE(string_agg(DISTINCT t.name, ', '), '') AS tags,
                COALESCE(
                    string_agg(
                        DISTINCT 'Input:\n' || COALESCE(tc.input, '') || '\nExpected:\n' || COALESCE(tc.expected_output, ''),
                        '\n\n---\n\n'
                    ),
                    ''
                ) AS sample_testcases,
                COALESCE(string_agg(DISTINCT pt.solution_signature, '\n'), '') AS solution_signatures
            FROM problems p
            LEFT JOIN problem_tags ptag ON ptag.problem_id = p.id
            LEFT JOIN tag t ON t.id = ptag.tag_id
            LEFT JOIN testsets ts ON ts.problem_id = p.id AND ts.is_active = true
            LEFT JOIN testcases tc ON tc.testset_id = ts.id AND tc.is_sample = true
            LEFT JOIN problem_templates pt ON pt.problem_id = p.id AND pt.is_active = true
            WHERE p.id = @problem_id
              AND p.is_active = true
            GROUP BY p.id;
            """;

        var row = await QuerySingleAsync(sql , new Dictionary<string , object?>
        {
            ["problem_id"] = problemId
        } , ct);

        if ( row is null )
            return null;

        return new AiEditorialContext
        {
            ProblemId = GetGuid(row , "problem_id")!.Value ,
            ProblemTitle = GetString(row , "problem_title") ,
            DescriptionMd = GetString(row , "description_md") ,
            Difficulty = GetString(row , "difficulty") ,
            ProblemMode = GetString(row , "problem_mode") ,
            TimeLimitMs = GetInt(row , "time_limit_ms") ,
            MemoryLimitKb = GetInt(row , "memory_limit_kb") ,
            StatusCode = GetString(row , "status_code") ,
            VisibilityCode = GetString(row , "visibility_code") ,
            Tags = GetString(row , "tags") ,
            SampleTestcases = GetString(row , "sample_testcases") ,
            SolutionSignatures = GetString(row , "solution_signatures")
        };
    }

    public async Task<AiEditorialCachedDraft?> GetCachedDraftAsync(
        Guid problemId ,
        string contextHash ,
        string languageCode ,
        string styleCode ,
        CancellationToken ct = default)
    {
        var sql =
            """
            SELECT id, problem_id, draft_status_code, language_code, style_code, title,
                   content_md, response_json, warnings_json, created_at
            FROM ai_editorial_drafts
            WHERE problem_id = @problem_id
              AND context_hash = @context_hash
              AND language_code = @language_code
              AND style_code = @style_code
              AND draft_status_code <> 'archived'
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        var row = await QuerySingleAsync(sql , new Dictionary<string , object?>
        {
            ["problem_id"] = problemId ,
            ["context_hash"] = contextHash ,
            ["language_code"] = languageCode ,
            ["style_code"] = styleCode
        } , ct);

        if ( row is null )
            return null;

        return new AiEditorialCachedDraft(
            DraftId: GetGuid(row , "id")!.Value ,
            ProblemId: GetGuid(row , "problem_id")!.Value ,
            DraftStatusCode: GetString(row , "draft_status_code") ?? "draft" ,
            LanguageCode: GetString(row , "language_code") ?? "vi" ,
            StyleCode: GetString(row , "style_code") ?? "educational" ,
            Title: GetString(row , "title") ?? "" ,
            ContentMd: GetString(row , "content_md") ?? "" ,
            ResponseJson: Convert.ToString(row["response_json"]) ,
            WarningsJson: Convert.ToString(row["warnings_json"]) ,
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

    public Task<Guid> InsertEditorialDraftAsync(
        AiEditorialDraftCreateDto dto ,
        CancellationToken ct = default)
    {
        var sql =
            """
            INSERT INTO ai_editorial_drafts (
                problem_id, requested_by_user_id, ai_request_log_id, context_hash,
                language_code, style_code, draft_status_code, title, summary_md,
                content_md, confidence_level_code, confidence_score,
                warnings_json, assumptions_json, response_json
            )
            VALUES (
                @problem_id, @user_id, @request_log_id, @context_hash,
                @language_code, @style_code, 'draft', @title, @summary_md,
                @content_md, @confidence_level_code, @confidence_score,
                CAST(@warnings_json AS jsonb), CAST(@assumptions_json AS jsonb), CAST(@response_json AS jsonb)
            )
            RETURNING id;
            """;

        return ExecuteScalarGuidAsync(sql , new Dictionary<string , object?>
        {
            ["problem_id"] = dto.ProblemId ,
            ["user_id"] = dto.RequestedByUserId ,
            ["request_log_id"] = dto.AiRequestLogId ,
            ["context_hash"] = dto.ContextHash ,
            ["language_code"] = dto.LanguageCode ,
            ["style_code"] = dto.StyleCode ,
            ["title"] = dto.Title ,
            ["summary_md"] = dto.SummaryMd ,
            ["content_md"] = dto.ContentMd ,
            ["confidence_level_code"] = dto.ConfidenceLevelCode ,
            ["confidence_score"] = dto.ConfidenceScore ,
            ["warnings_json"] = dto.WarningsJson ,
            ["assumptions_json"] = dto.AssumptionsJson ,
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