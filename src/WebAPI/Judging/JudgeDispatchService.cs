using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace WebAPI.Judging;

public sealed class JudgeDispatchService
{
    private readonly JudgeConnectionRegistry _registry;
    private readonly ILogger<JudgeDispatchService> _logger;
    private readonly ConcurrentDictionary<int , SubmissionState> _submissions = new();

    public JudgeDispatchService(
        JudgeConnectionRegistry registry ,
        ILogger<JudgeDispatchService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public IReadOnlyCollection<SubmissionState> GetAllSubmissions()
    {
        return _submissions.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList()
            .AsReadOnly();
    }

    public SubmissionState? GetSubmission(int submissionId)
    {
        _submissions.TryGetValue(submissionId , out var state);
        return state;
    }

    public async Task<object> DispatchSubmissionAsync(
        SubmissionRequestModel request ,
        CancellationToken cancellationToken)
    {
        KeyValuePair<string , TcpClient> selectedJudge;

        if ( !string.IsNullOrWhiteSpace(request.JudgeId) )
        {
            if ( !_registry.TryGet(request.JudgeId , out var judgeClient) )
            {
                throw new InvalidOperationException($"Judge '{request.JudgeId}' is offline.");
            }

            selectedJudge = new KeyValuePair<string , TcpClient>(request.JudgeId , judgeClient);
        }
        else
        {
            if ( !_registry.TryGetAny(out selectedJudge) )
            {
                throw new InvalidOperationException("No online judge available.");
            }
        }

        var submission = new SubmissionState
        {
            SubmissionId = request.SubmissionId ,
            ProblemId = request.ProblemId ,
            Language = request.Language ,
            Status = "Dispatched" ,
            IsDone = false ,
            CreatedAtUtc = DateTime.UtcNow ,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _submissions[request.SubmissionId] = submission;

        var normalizedPacket = new Dictionary<string , object?>
        {
            ["name"] = "submission-request" ,
            ["submission-id"] = request.SubmissionId ,
            ["problem-id"] = request.ProblemId ,
            ["language"] = request.Language ,
            ["source"] = request.Source ,
            ["time-limit"] = request.TimeLimit ,
            ["memory-limit"] = request.MemoryLimit ,
            ["short-circuit"] = request.ShortCircuit ,
            ["meta"] = new Dictionary<string , object?>
            {
                ["debug"] = true
            }
        };

        var stream = selectedJudge.Value.GetStream();
        await BridgePacketCodec.WritePacketAsync(stream , normalizedPacket , cancellationToken);

        _logger.LogInformation(
            "Submission {SubmissionId} dispatched to judge {JudgeId} for problem {ProblemId} using language {Language}" ,
            request.SubmissionId ,
            selectedJudge.Key ,
            request.ProblemId ,
            request.Language);

        return new
        {
            request.SubmissionId ,
            request.ProblemId ,
            request.Language ,
            JudgeId = selectedJudge.Key ,
            Status = submission.Status
        };
    }

    public void HandlePacket(string judgeId , Dictionary<string , JsonElement> packet)
    {
        if ( !packet.TryGetValue("name" , out var nameElement) )
            return;

        var packetName = nameElement.GetString();
        if ( string.IsNullOrWhiteSpace(packetName) )
            return;

        var rawJson = JsonSerializer.Serialize(packet);
        _logger.LogInformation("Raw packet from judge {JudgeId}: {RawPacket}" , judgeId , rawJson);

        switch ( packetName )
        {
            case "submission-acknowledged":
                HandleSubmissionAcknowledged(packet);
                break;

            case "grading-begin":
                HandleGradingBegin(packet);
                break;

            case "test-case-status":
                HandleTestCaseStatus(packet);
                break;

            case "grading-end":
                HandleGradingEnd(packet);
                break;

            case "compile-error":
                HandleCompileError(packet);
                break;

            case "compile-message":
                HandleCompileMessage(packet);
                break;

            case "internal-error":
                HandleInternalError(packet);
                break;
        }

        _logger.LogInformation("Packet from judge {JudgeId}: {PacketName}" , judgeId , packetName);
    }

    private void HandleSubmissionAcknowledged(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.Status = "Acknowledged";
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleGradingBegin(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.Status = "Grading";
            state.IsPretested = GetBool(packet , "pretested") ?? false;
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleTestCaseStatus(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( !_submissions.TryGetValue(submissionId.Value , out var state) )
            return;

        if ( !packet.TryGetValue("cases" , out var casesElement) || casesElement.ValueKind != JsonValueKind.Array )
            return;

        foreach ( var caseElement in casesElement.EnumerateArray() )
        {
            var caseNumber =
                GetInt(caseElement , "position")
                ?? GetInt(caseElement , "case")
                ?? GetInt(caseElement , "test-case")
                ?? 0;

            var status = GetVerdict(caseElement);

            var caseState = state.Cases.FirstOrDefault(x => x.CaseNumber == caseNumber);
            if ( caseState is null )
            {
                caseState = new SubmissionCaseState
                {
                    CaseNumber = caseNumber
                };
                state.Cases.Add(caseState);
            }

            caseState.Status = status;
            caseState.Batch = GetInt(caseElement , "batch");
            caseState.Time = GetDouble(caseElement , "time");
            caseState.Memory = GetInt(caseElement , "memory");
            caseState.Points = GetInt(caseElement , "points");
            caseState.Total = GetInt(caseElement , "total-points") ?? GetInt(caseElement , "total");
            caseState.Output =
                GetString(caseElement , "output")
                ?? GetString(caseElement , "feedback")
                ?? GetString(caseElement , "extended-feedback");

            state.CurrentCase = caseNumber;
            state.Batch = caseState.Batch;
            state.Time = caseState.Time;
            state.Memory = caseState.Memory;
            state.Points = caseState.Points;
            state.Total = caseState.Total;
            state.Output = caseState.Output;
            state.Status = status;
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleGradingEnd(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.IsDone = true;
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleCompileError(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.IsDone = true;
            state.Status = "CE";
            state.Output = GetString(packet , "log") ?? GetString(packet , "message");
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleCompileMessage(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.Output = GetString(packet , "log") ?? GetString(packet , "message");
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void HandleInternalError(Dictionary<string , JsonElement> packet)
    {
        var submissionId = GetInt(packet , "submission-id");
        if ( submissionId is null )
            return;

        if ( _submissions.TryGetValue(submissionId.Value , out var state) )
        {
            state.IsDone = true;
            state.Status = "IE";
            state.Output =
                GetString(packet , "message")
                ?? GetString(packet , "log")
                ?? "Internal error from judge.";
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private static string GetVerdict(JsonElement caseElement)
    {
        var textVerdict =
            GetString(caseElement , "verdict")
            ?? GetString(caseElement , "result");

        if ( !string.IsNullOrWhiteSpace(textVerdict) )
            return textVerdict!;

        var numericStatus = GetInt(caseElement , "status");
        if ( numericStatus is null )
            return "Unknown";

        return numericStatus.Value switch
        {
            0 => "AC",
            1 => "WA",
            2 => "RTE",
            3 => "TLE",
            4 => "MLE",
            5 => "OLE",
            6 => "IE",
            _ => $"STATUS_{numericStatus.Value}"
        };
    }

    private static int? GetInt(Dictionary<string , JsonElement> packet , string key)
    {
        if ( !packet.TryGetValue(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) )
            return result;

        if ( value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString() , out result) )
            return result;

        return null;
    }

    private static double? GetDouble(Dictionary<string , JsonElement> packet , string key)
    {
        if ( !packet.TryGetValue(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result) )
            return result;

        if ( value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString() , out result) )
            return result;

        return null;
    }

    private static bool? GetBool(Dictionary<string , JsonElement> packet , string key)
    {
        if ( !packet.TryGetValue(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.True ) return true;
        if ( value.ValueKind == JsonValueKind.False ) return false;

        if ( value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString() , out var result) )
            return result;

        return null;
    }

    private static string? GetString(Dictionary<string , JsonElement> packet , string key)
    {
        if ( !packet.TryGetValue(key , out var value) )
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static int? GetInt(JsonElement element , string key)
    {
        if ( !element.TryGetProperty(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) )
            return result;

        if ( value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString() , out result) )
            return result;

        return null;
    }

    private static double? GetDouble(JsonElement element , string key)
    {
        if ( !element.TryGetProperty(key , out var value) )
            return null;

        if ( value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result) )
            return result;

        if ( value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString() , out result) )
            return result;

        return null;
    }

    private static string? GetString(JsonElement element , string key)
    {
        if ( !element.TryGetProperty(key , out var value) )
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }
}