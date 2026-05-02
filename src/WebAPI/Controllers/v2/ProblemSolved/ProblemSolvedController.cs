using Application.UseCases.ProblemSolved.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v2.ProblemSolved;

[ApiController]
[ApiVersion("2.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProblemSolvedController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemSolvedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMySolvedStats(
        [FromQuery] string? visibilityCode ,
        [FromQuery] string? solvedSourceCode ,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyProblemSolvedStatsQuery(
                visibilityCode ,
                solvedSourceCode) ,
            cancellationToken);

        return Ok(result);
    }
}