using Application.UseCases.Contests.Commands;
using Application.UseCases.Contests.Dtos;
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
        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(result, "Contest created successfully"));
    }

    // =============================================
    // GET CONTEST LIST
    // =============================================
    [HttpGet]
    public async Task<IActionResult> GetContests(
        [FromQuery] GetContestsQuery query,
        CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);

        return Ok(ApiResponse<object>.Ok(result, "Fetched contests successfully"));
    }

    // =============================================
    // GET CONTEST DETAIL
    // =============================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContestDetail(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetContestDetailQuery(id), ct);

        return Ok(ApiResponse<ContestDetailDto>.Ok(
            result,
            "Fetched contest detail successfully"
        ));
    }
    // =============================================
    // GET MY CONTESTS
    // =============================================
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyContests(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetMyContestsQuery { Status = status }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Fetched my contests successfully"
        ));
    }

    // =============================================
    // UPDATE CONTEST
    // =============================================
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> UpdateContest(
        Guid id,
        [FromBody] UpdateContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT UPDATE CONTEST ===");

            command.ContestId = id;

            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Contest updated successfully"
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== UPDATE ERROR ===");
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
    // JOIN CONTEST (ONLY WHEN RUNNING)
    // =============================================
    [HttpPost("{contestId:guid}/join")]
    [Authorize]
    public async Task<IActionResult> JoinContest(
        Guid contestId,
        [FromBody] JoinContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT JOIN CONTEST ===");

            // bind route → command
            command.ContestId = contestId;

            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Joined contest successfully"
            ));
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
    // REGISTER (BEFORE 8 HOURS)
    // =============================================
    [HttpPost("{contestId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> Register(
        Guid contestId,
        [FromBody] RegisterContestCommand command,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT REGISTER CONTEST ===");

            command.ContestId = contestId;

            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Registered successfully"
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== REGISTER ERROR ===");
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
    // UNREGISTER (BEFORE 4 HOURS)
    // =============================================
    [HttpDelete("{contestId:guid}/unregister")]
    [Authorize]
    public async Task<IActionResult> Unregister(
        Guid contestId,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine("=== HIT UNREGISTER CONTEST ===");

            var result = await _mediator.Send(
                new UnregisterContestCommand
                {
                    ContestId = contestId
                }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Unregistered successfully"
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== UNREGISTER ERROR ===");
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
    // GET SCOREBOARD
    // =============================================
    [HttpGet("{contestId:guid}/scoreboard")]
    public async Task<IActionResult> GetScoreboard(
        Guid contestId,
        CancellationToken ct)
    {
        if (contestId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "contestId is required"
            });
        }

        try
        {
            Console.WriteLine("=== HIT SCOREBOARD ===");
            Console.WriteLine($"ContestId: {contestId}");

            var result = await _mediator.Send(
                new GetContestLeaderboardQuery
                {
                    ContestId = contestId
                },
                ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched scoreboard successfully"
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== SCOREBOARD ERROR ===");
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
    // ADD PROBLEM
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
    // GET PROBLEMS
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
    // SUBMIT
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

            command.ContestId = contestId;

            var result = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(result, "Submitted successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== SUBMIT ERROR ===");

            return BadRequest(new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace
            });
        }
    }

    // =============================================
    // PUBLISH
    // =============================================
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new PublishContestCommand(id), ct);

        return Ok(new
        {
            success = true,
            data = result,
            message = "Contest has been successfully published"
        });
    }

    // =============================================
    // HEALTH CHECK
    // =============================================
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("OK");
    }
    // =============================================
    // GET MY TEAM IN CONTEST
    // =============================================

    [HttpGet("{contestId}/my-team")]
    [Authorize]
    public async Task<IActionResult> GetMyTeam(Guid contestId)
    {
        var result = await _mediator.Send(
            new GetMyTeamInContestQuery
            {
                ContestId = contestId
            });

        return Ok(ApiResponse<object?>.Ok(
            result,
            "Fetched my team in contest"
        ));
    }
    // =============================================
    // FREEZE SCOREBOARD
    // =============================================
    [HttpPost("{id:guid}/freeze")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Freeze(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new FreezeContestCommand { ContestId = id }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Scoreboard frozen successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
    // =============================================
    // UNFREEZE SCOREBOARD
    // =============================================
    [HttpPost("{id:guid}/unfreeze")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Unfreeze(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UnfreezeContestCommand { ContestId = id }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Scoreboard unfrozen successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}