using Application.UseCases.Contests.Commands;
using Application.UseCases.Contests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    // =============================================
    [HttpPost]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateContest(
        [FromBody] CreateContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT CREATE CONTEST ===");

            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(result, "Contest created successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR CREATE: {ex}");

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }

    // =============================================
    // GET CONTEST LIST
    // =============================================
    [HttpGet]
    public async Task<IActionResult> GetContests(
        [FromQuery] GetContestsQuery query,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT GET CONTESTS ===");

            var result = await _mediator.Send(query, ct);

            return Ok(ApiResponse<object>.Ok(result, "Fetched contests successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR GET LIST: {ex}");

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }

    // =============================================
    // GET CONTEST DETAIL
    // =============================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContestDetail(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"=== HIT GET DETAIL: {id} ===");

            var result = await _mediator.Send(
                new GetContestDetailQuery(id),
                ct
            );

            return Ok(ApiResponse<object>.Ok(result, "Fetched contest detail successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR DETAIL: {ex}");

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }

    // =============================================
    // JOIN CONTEST (DEBUG FULL)
    // =============================================
    [HttpPost("{id:guid}/join")]
    [Authorize]
    public async Task<IActionResult> JoinContest(
        Guid id,
        [FromBody] JoinContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT JOIN CONTEST ===");
            Console.WriteLine($"Route ContestId: {id}");

            // 🔥 LOG BODY
            Console.WriteLine($"Body ContestId: {command.ContestId}");
            Console.WriteLine($"Body TeamId: {command.TeamId}");

            // 🔥 LOG USER
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"UserId from token: {userId}");

            // 🔥 ASSIGN FROM ROUTE
            command.ContestId = id;

            var result = await _mediator.Send(command, ct);

            Console.WriteLine("=== JOIN SUCCESS ===");

            return Ok(ApiResponse<object>.Ok(result, "Joined contest successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== JOIN ERROR ===");
            Console.WriteLine(ex.ToString());

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }
    [HttpGet("ping")]
public IActionResult Ping()
{
    return Ok("OK");
}
}