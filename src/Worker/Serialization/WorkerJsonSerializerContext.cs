using System.Text.Json.Serialization;
using Contracts.Submissions.Judging;

namespace Worker.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase ,
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
internal partial class WorkerJsonSerializerContext : JsonSerializerContext
{
}