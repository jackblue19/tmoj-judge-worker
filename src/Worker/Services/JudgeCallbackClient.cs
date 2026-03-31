//using System.Net.Http.Json;
//using Contracts.Submissions.Judging;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Worker.Serialization;

//namespace Worker.Services;

//public sealed class JudgeCallbackClient
//{
//    private readonly HttpClient _httpClient;
//    private readonly ILogger<JudgeCallbackClient> _logger;
//    private readonly string _baseUrl;
//    private readonly string _apiKey;

//    public JudgeCallbackClient(
//        HttpClient httpClient ,
//        IConfiguration configuration ,
//        ILogger<JudgeCallbackClient> logger)
//    {
//        _httpClient = httpClient;
//        _logger = logger;
//        _baseUrl = configuration["Judge:CallbackBaseUrl"]
//            ?? throw new InvalidOperationException("Judge:CallbackBaseUrl is missing.");
//        _apiKey = configuration["Judge:ApiKey"]
//            ?? throw new InvalidOperationException("Judge:ApiKey is missing.");
//    }

//    public async Task SendJobCompletedAsync(
//        JudgeJobCompletedContract payload ,
//        CancellationToken ct)
//    {
//        var url = $"{_baseUrl.TrimEnd('/')}/api/internal/judge/callbacks/job-completed";

//        for ( var attempt = 1; attempt <= 3; attempt++ )
//        {
//            try
//            {
//                using var req = new HttpRequestMessage(HttpMethod.Post , url);
//                req.Headers.Add("X-API-KEY" , _apiKey);
//                //req.Content = JsonContent.Create(payload);
//                req.Content = JsonContent.Create(
//                    payload ,
//                    WorkerJsonSerializerContext.Default.JudgeJobCompletedContract);

//                using var res = await _httpClient.SendAsync(req , ct);
//                var body = await res.Content.ReadAsStringAsync(ct);

//                if ( res.IsSuccessStatusCode )
//                {
//                    _logger.LogInformation(
//                        "Judge callback success. JobId={JobId}, SubmissionId={SubmissionId}, Attempt={Attempt}" ,
//                        payload.JobId , payload.SubmissionId , attempt);
//                    return;
//                }

//                _logger.LogWarning(
//                    "Judge callback failed. StatusCode={StatusCode}, Body={Body}, Attempt={Attempt}" ,
//                    (int) res.StatusCode , body , attempt);
//            }
//            catch ( Exception ex ) when ( attempt < 3 )
//            {
//                _logger.LogWarning(ex ,
//                    "Judge callback exception. JobId={JobId}, Attempt={Attempt}" ,
//                    payload.JobId , attempt);
//            }

//            await Task.Delay(TimeSpan.FromSeconds(attempt) , ct);
//        }

//        throw new InvalidOperationException(
//            $"Judge callback failed after retries. JobId={payload.JobId}, SubmissionId={payload.SubmissionId}");
//    }
//}