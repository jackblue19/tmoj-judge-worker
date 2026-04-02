using System.Net;
using System.Net.Http.Json;
using Contracts.Submissions.Judging;
using Worker.Serialization;

namespace Worker.Services;

public sealed class JudgeBackendClient
{
    private readonly HttpClient _http;

    public JudgeBackendClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Guid> RegisterWorkerAsync(
        JudgeWorkerRegistrationContract req ,
        CancellationToken ct)
    {
        using var res = await _http.PostAsJsonAsync(
            "api/internal/judge/workers/register" ,
            req ,
            cancellationToken: ct);

        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<RegisterWorkerResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Register worker response is empty.");

        return payload.WorkerId;
    }

    public async Task HeartbeatAsync(
        JudgeWorkerHeartbeatContract req ,
        CancellationToken ct)
    {
        using var res = await _http.PostAsJsonAsync(
            "api/internal/judge/workers/heartbeat" ,
            req ,
            cancellationToken: ct);

        res.EnsureSuccessStatusCode();
    }

    public async Task<DispatchJudgeJobContract?> ClaimNextAsync(Guid workerId , CancellationToken ct)
    {
        using var res = await _http.PostAsync(
            $"api/internal/judge/jobs/claim-next?workerId={workerId}" ,
            content: null ,
            cancellationToken: ct);

        if ( res.StatusCode == HttpStatusCode.NoContent )
            return null;

        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync(
            WorkerJsonSerializerContext.Default.DispatchJudgeJobContract ,
            ct);
    }

    public async Task CompleteAsync(JudgeJobCompletedContract req , CancellationToken ct)
    {
        using var res = await _http.PostAsJsonAsync(
            "api/internal/judge/jobs/complete" ,
            req ,
            WorkerJsonSerializerContext.Default.JudgeJobCompletedContract ,
            ct);

        res.EnsureSuccessStatusCode();
    }

    private sealed class RegisterWorkerResponse
    {
        public Guid WorkerId { get; set; }
    }
}