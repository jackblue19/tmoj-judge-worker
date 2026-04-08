using Application.UseCases.Contests.Commands;
using Application.UseCases.Contests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Contests;

[ApiController]
[Route("api/v1/contests")]
[Tags("Contests")]
public class ContestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =============================================
    // CREATE CONTEST
    // POST: /api/v1/contests
    // =============================================
    [HttpPost]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateContest(
        [FromBody] CreateContestCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Contest created successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                detail = ex.InnerException?.InnerException?.Message
            });
        }
    }
    // =============================================
    // GET CONTEST LIST
    // GET: /api/v1/contests
    // =============================================
    [HttpGet]
    public async Task<IActionResult> GetContests(
        [FromQuery] GetContestsQuery query,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(query, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched contests successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                detail = ex.InnerException?.InnerException?.Message
            });
        }
    }
    // =============================================
    // GET CONTEST DETAIL
    // GET: /api/v1/contests/{id}
    // =============================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContestDetail(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetContestDetailQuery(id),
                ct
            );

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched contest detail successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }
}