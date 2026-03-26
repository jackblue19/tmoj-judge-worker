using Application.Abstractions.Outbound.Services;
using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed class GetProblemStatementAccessQueryHandler
    : IRequestHandler<GetProblemStatementAccessQuery , GetProblemStatementAccessDto>
{
    private readonly IProblemRepository _repo;
    private readonly IR2Service _r2;
    private readonly ILogger<GetProblemStatementAccessQueryHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetProblemStatementAccessQueryHandler(
        ILogger<GetProblemStatementAccessQueryHandler> logger ,
        IProblemRepository repo ,
        IR2Service r2 ,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _repo = repo;
        _r2 = r2;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GetProblemStatementAccessDto> Handle(
        GetProblemStatementAccessQuery request ,
        CancellationToken ct)
    {
        var problem = await _repo.GetByIdAsync(request.ProblemId , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        // 1) Inline markdown
        if ( problem.StatementSourceCode == "inline_md" )
        {
            return new GetProblemStatementAccessDto
            {
                Mode = "inline" ,
                Bytes = Encoding.UTF8.GetBytes(problem.DescriptionMd ?? string.Empty) ,
                ContentType = "text/markdown; charset=utf-8"
            };
        }

        // 2) File on R2
        if ( problem.StatementFileId is null )
            throw new KeyNotFoundException("Problem does not have a statement file.");

        var url = await _r2.GetPresignedUrlAsync(
            "Problem" ,
            problem.StatementFileId.Value ,
            TimeSpan.FromMinutes(3) ,
            ct);

        _logger.LogInformation("Problem statement presigned url: {Url}" , url);

        var http = _httpClientFactory.CreateClient();

        using var response = await http.GetAsync(
            url ,
            HttpCompletionOption.ResponseHeadersRead ,
            ct);

        if ( !response.IsSuccessStatusCode )
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to download problem statement from presigned url. Status: {StatusCode}, Body: {Body}" ,
                (int) response.StatusCode ,
                body);

            throw new InvalidOperationException("Unable to fetch statement file from storage.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        var contentType =
            !string.IsNullOrWhiteSpace(problem.StatementContentType)
                ? problem.StatementContentType
                : response.Content.Headers.ContentType?.ToString()
                    ?? "application/octet-stream";

        return new GetProblemStatementAccessDto
        {
            Mode = "inline" ,
            Bytes = bytes ,
            ContentType = contentType
        };
    }
}