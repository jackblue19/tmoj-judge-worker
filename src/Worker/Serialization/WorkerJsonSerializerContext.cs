using System.Text.Json.Serialization;
using Contracts.Submissions.Judging;

namespace Worker.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase ,
    PropertyNameCaseInsensitive = true ,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JudgeWorkerHeartbeatContract))]
[JsonSerializable(typeof(JudgeJobCompletedContract))]
[JsonSerializable(typeof(JudgeCompileResultContract))]
[JsonSerializable(typeof(JudgeSummaryResultContract))]
[JsonSerializable(typeof(JudgeCaseCompletedContract))]
[JsonSerializable(typeof(List<JudgeCaseCompletedContract>))]
[JsonSerializable(typeof(DispatchJudgeJobContract))]
[JsonSerializable(typeof(DispatchJudgeCaseContract))]
[JsonSerializable(typeof(List<DispatchJudgeCaseContract>))]
[JsonSerializable(typeof(RegisterWorkerResponse))]
[JsonSerializable(typeof(JudgeWorkerRegistrationContract))]
internal partial class WorkerJsonSerializerContext : JsonSerializerContext
{
}

public sealed class RegisterWorkerResponse
{
    public Guid WorkerId { get; set; }
}