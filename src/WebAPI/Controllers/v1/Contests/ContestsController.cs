using Application.UseCases.Contests.Commands;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using Application.UseCases.Teams.Commands;
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
        command.ContestId = id;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Contest updated successfully"
        ));
    }

    // =============================================
    // JOIN CONTEST
    // =============================================
    [HttpPost("{contestId:guid}/join")]
    [Authorize]
    public async Task<IActionResult> JoinContest(
        Guid contestId,
        [FromBody] JoinContestCommand command,
        CancellationToken ct)
    {
        command.ContestId = contestId;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Joined contest successfully"
        ));
    }

    // =============================================
    // REGISTER
    // =============================================
    [HttpPost("{contestId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> Register(
        Guid contestId,
        [FromBody] RegisterContestCommand command,
        CancellationToken ct)
    {
        command.ContestId = contestId;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Registered successfully"
        ));
    }

    // =============================================
    // UNREGISTER
    // =============================================
    [HttpDelete("{contestId:guid}/unregister")]
    [Authorize]
    public async Task<IActionResult> Unregister(
        Guid contestId,
        CancellationToken ct)
    {
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

    // =============================================
    // SCOREBOARD (FREEZE APPLY HERE ONLY)
    // =============================================
    [HttpGet("{contestId:guid}/scoreboard")]
    public async Task<IActionResult> GetScoreboard(
        Guid contestId,
        CancellationToken ct)
    {
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
    // GET PROBLEMS (NO FREEZE BLOCK)
    // =============================================
    [HttpGet("{contestId}/problems")]
    public async Task<IActionResult> GetProblems(Guid contestId)
    {
        var result = await _mediator.Send(
            new GetContestProblemsQuery(contestId));

        return Ok(ApiResponse<object>.Ok(
            result,
            "Fetched contest problems successfully"
        ));
    }

    // =============================================
    // SUBMIT (🔥 KHÔNG BLOCK KHI FREEZE)
    // =============================================
    [HttpPost("{contestId}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(
        Guid contestId,
        [FromBody] SubmitContestCommand command,
        CancellationToken ct)
    {
        command.ContestId = contestId;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Submitted successfully"
        ));
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

        return Ok(ApiResponse<object>.Ok(
            result,
            "Contest has been successfully published"
        ));
    }

    // =============================================
    // GET MY TEAM
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
        var result = await _mediator.Send(
            new FreezeContestCommand { ContestId = id }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Scoreboard frozen successfully"
        ));
    }

    // =============================================
    // UNFREEZE SCOREBOARD
    // =============================================
    [HttpPost("{id:guid}/unfreeze")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Unfreeze(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UnfreezeContestCommand { ContestId = id }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Scoreboard unfrozen successfully"
        ));
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
    // REMIX CONTEST
    // =============================================
    [HttpPost("{id:guid}/remix")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> RemixContest(
    Guid id,
    CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RemixContestCommand
            {
                SourceContestId = id   // 🔥 FIX
            }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Contest remixed successfully"
        ));
    }

    // =============================================
    // TEAMS - JOIN BY INVITE CODE
    // =============================================
    [HttpPost("{contestId:guid}/teams/join-by-code")]
    [Authorize]
    public async Task<IActionResult> JoinTeamByCode(
        Guid contestId,
        [FromBody] JoinTeamByCodeCommand command,
        CancellationToken ct)
    {
        await _mediator.Send(command, ct);

        return Ok(ApiResponse<object?>.Ok(null, "Joined team successfully"));
    }

    // =============================================
    // TEAMS - CREATE INVITE CODE
    // =============================================
    [HttpPost("{contestId:guid}/teams/invite-code")]
    [Authorize]
    public async Task<IActionResult> CreateTeamInviteCode(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateTeamInviteCodeCommand { ContestId = contestId }, ct);

        return Ok(ApiResponse<string>.Ok(result, "Invite code created successfully"));
    }

    // =============================================
    // TEAMS - GET INVITE CODE
    // =============================================
    [HttpGet("{contestId:guid}/teams/invite-code")]
    [Authorize]
    public async Task<IActionResult> GetTeamInviteCode(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTeamInviteCodeQuery { ContestId = contestId }, ct);

        return Ok(ApiResponse<string?>.Ok(result, "Fetched team invite code successfully"));
    }

    // =============================================
    // CHANGE VISIBILITY
    // =============================================
    [HttpPut("{contestId:guid}/visibility")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ChangeVisibility(
        Guid contestId,
        [FromBody] ChangeVisibilityCommand command,
        CancellationToken ct)
    {
        command.ContestId = contestId;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Visibility changed successfully"
        ));
    }

    // =============================================
    // ARCHIVE CONTEST
    // =============================================
    [HttpPost("{contestId:guid}/archive")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ArchiveContest(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ArchiveContestCommand { ContestId = contestId }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Contest archived successfully"
        ));
    }

    // =============================================
    // CREATE VIRTUAL CONTEST
    // =============================================
    [HttpPost("{contestId:guid}/virtual")]
    [Authorize]
    public async Task<IActionResult> CreateVirtualContest(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateVirtualContestCommand
            {
                SourceContestId = contestId
            }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Virtual contest created successfully"
        ));
    }

    // =============================================
    // PUBLISH FINAL RANKING
    // =============================================
    [HttpPost("{contestId:guid}/ranking/publish")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> PublishRanking(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new PublishRankingCommand { ContestId = contestId }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Ranking published successfully"
        ));
    }

    // =============================================
    // RECALCULATE CONTEST RESULTS
    // =============================================
    [HttpPost("{contestId:guid}/recalculate")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> RecalculateContest(
        Guid contestId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RecalculateContestCommand { ContestId = contestId }, ct);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Recalculation job enqueued successfully"
        ));
    }
}