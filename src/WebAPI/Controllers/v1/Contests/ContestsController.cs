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

    // =============================================
    // GET PING (HEALTH CHECK)
    // =============================================

    [HttpGet("ping")]
public IActionResult Ping()
{
    return Ok("OK");
}

    // =============================================
    // GET LEADERBOARD 
    // =============================================

    [HttpGet("{contestId}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid contestId)
    {
        var result = await _mediator.Send(new GetContestLeaderboardQuery
        {
            ContestId = contestId
        });

        return Ok(result);
    }
    // =============================================
    // POST ADD PROBLEM TO CONTEST
    // =============================================

    [HttpPost("{contestId}/problems")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> AddProblemToContest(
    Guid contestId,
    [FromBody] AddContestProblemCommand command,
    CancellationToken ct)
    {
        command = command with { ContestId = contestId };

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<Guid>.Ok(result, "Added problem to contest"));
    }

    // =============================================
    // GET CONTEST PROBLEMS
    // =============================================

    [HttpGet("{contestId}/problems")]
    public async Task<IActionResult> GetProblems(Guid contestId)
    {
        var result = await _mediator.Send(
            new GetContestProblemsQuery(contestId));

        return Ok(new
        {
            data = result,
            message = "Fetched contest problems successfully"
        });
    }
    // =============================================
    // POST SUBMIT CONTEST (DEBUG FULL)
    // =============================================
    [HttpPost("{contestId}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(
        Guid contestId,
        [FromBody] SubmitContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT SUBMIT CONTEST ===");
            Console.WriteLine($"ContestId: {contestId}");
            Console.WriteLine($"ContestProblemId: {command.ContestProblemId}");
            Console.WriteLine($"Code length: {command.Code?.Length}");

            command.ContestId = contestId;

            var result = await _mediator.Send(command, ct);

            Console.WriteLine("=== SUBMIT SUCCESS ===");

            return Ok(ApiResponse<object>.Ok(result, "Submitted successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== SUBMIT ERROR ===");
            Console.WriteLine(ex.ToString());

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }
}