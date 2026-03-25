using Application.Abstractions.Outbound.Services;
using Application.UseCases.Problems.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed class GetProblemStatementAccessQueryHandler
    : IRequestHandler<GetProblemStatementAccessQuery , GetProblemStatementAccessDto>
{
    private readonly IProblemRepository _repo;
    private readonly IR2Service _r2;
    private readonly ILogger<IR2Service> _logger;

    public GetProblemStatementAccessQueryHandler(
        ILogger<IR2Service> logger ,
        IProblemRepository repo ,
        IR2Service r2)
    {
        _logger = logger;
        _repo = repo;
        _r2 = r2;
    }

    public async Task<GetProblemStatementAccessDto> Handle(
        GetProblemStatementAccessQuery request ,
        CancellationToken ct)
    {
        var problem = await _repo.GetByIdAsync(request.ProblemId , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        // 🔹 inline markdown
        if ( problem.StatementSourceCode == "inline_md" )
        {
            return new GetProblemStatementAccessDto
            {
                Mode = "inline" ,
                Bytes = Encoding.UTF8.GetBytes(problem.DescriptionMd ?? "") ,
                ContentType = "text/markdown; charset=utf-8"
            };
        }

        // 🔹 file on R2
        var url = await _r2.GetPresignedUrlAsync(
            "Problem" ,
            problem.StatementFileId!.Value ,
            TimeSpan.FromMinutes(3) ,
            ct);

        _logger.LogInformation("Problem statement presigned url: {Url}" , url);

        return new GetProblemStatementAccessDto
        {
            Mode = "redirect" ,
            Url = url
        };
    }
}
