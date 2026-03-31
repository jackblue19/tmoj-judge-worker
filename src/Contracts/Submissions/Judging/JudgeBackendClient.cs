using System.Net.Http.Json;
using Contracts.Submissions.Judging;

public sealed class JudgeBackendClient
{
    private readonly HttpClient _http;

    public JudgeBackendClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<DispatchJudgeJobContract?> ClaimNextAsync(Guid workerId , CancellationToken ct)
    {
        var res = await _http.PostAsync(
            $"api/internal/judge/jobs/claim-next?workerId={workerId}" ,
            content: null ,
            cancellationToken: ct);

        if ( res.StatusCode == System.Net.HttpStatusCode.NoContent )
            return null;

        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<DispatchJudgeJobContract>(cancellationToken: ct);
    }

    public async Task CompleteAsync(JudgeJobCompletedContract req , CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync(
            "api/internal/judge/jobs/complete" ,
            req ,
            cancellationToken: ct);

        res.EnsureSuccessStatusCode();
    }
}