using System.Text.Json.Serialization;
using Contracts.Submissions.Judging;

namespace Worker.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase ,
    PropertyNameCaseInsensitive = true ,
    WriteIndented = false)]
[JsonSerializable(typeof(DispatchJudgeJobContract))]
[JsonSerializable(typeof(JudgeJobCompletedContract))]
[JsonSerializable(typeof(JudgeWorkerHeartbeatContract))]
internal partial class JudgeContractsJsonContext : JsonSerializerContext
{
}