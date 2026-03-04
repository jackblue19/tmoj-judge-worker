using Application.UseCases.Problems.Commands.CreateProblem;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Queries.GetAllProblems;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v2.ProblemManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProblemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [OutputCache(
        PolicyName = "ProblemsList" ,
        VaryByQueryKeys = new[] { "difficulty" , "status" , "page" , "pageSize" }
    )]
    public async Task<ActionResult<ApiPagedResponse<ProblemListItemDto>>> GetAll(
        [FromQuery] string? difficulty ,
        [FromQuery] string? status ,
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProblemsQuery(difficulty , status , page , pageSize) ,
            ct);

        var pagination = new PaginationMeta(
            result.Page ,
            result.PageSize ,
            result.TotalCount ,
            (long) Math.Ceiling(result.TotalCount / (double) result.PageSize) ,
            result.Page > 1 ,
            result.Page * result.PageSize < result.TotalCount
        );

        return Ok(ApiPagedResponse<ProblemListItemDto>.Ok(
            result.Items ,
            pagination ,
            traceId: HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
    [FromBody] CreateProblemCommand command ,
    CancellationToken ct)
    {
        var id = await _mediator.Send(command , ct);

        return CreatedAtAction(
            "nameof(GetById)" ,
            new { id } ,
            new { id });
    }
}
