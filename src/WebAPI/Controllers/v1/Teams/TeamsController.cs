using Application.UseCases.Teams.Commands;
using Application.UseCases.Teams.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Teams;

[ApiController]
[Route("api/v1/teams")]
[Tags("Teams")]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================
    // CREATE TEAM
    // =========================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateTeamCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<CreateTeamResponse>.Ok(result, "Team created successfully"));
    }

    // =========================
    // GET DETAIL
    // =========================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTeamDetailQuery(id), ct);

        return Ok(ApiResponse<object?>.Ok(result, "Fetched team detail"));
    }

    // =========================
    // GET ALL TEAMS
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTeamsQuery(), ct);

        return Ok(ApiResponse<object?>.Ok(result, "Fetched teams"));
    }

    // =========================
    // ADD MEMBER
    // =========================
    [HttpPost("{id:guid}/members")]
    [Authorize]
    public async Task<IActionResult> AddMember(Guid id, AddTeamMemberCommand command, CancellationToken ct)
    {
        command.TeamId = id;

        var result = await _mediator.Send(command, ct);

        return Ok(ApiResponse<Guid>.Ok(result, "Member added"));
    }

    // =========================
    // REMOVE MEMBER
    // =========================
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveTeamMemberCommand
        {
            TeamId = id,
            UserId = userId
        }, ct);

        return Ok(ApiResponse<object?>.Ok(null, "Member removed"));
    }

    // =========================
    // JOIN BY CODE
    // =========================
    [HttpPost("join-by-code")]
    [Authorize]
    public async Task<IActionResult> JoinByCode(JoinTeamByCodeCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);

        return Ok(ApiResponse<object?>.Ok(null, "Joined team"));
    }

    // =========================
    // DELETE TEAM
    // =========================
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTeamCommand(id), ct);

        return Ok(ApiResponse<object?>.Ok(null, "Team deleted"));
    }
}