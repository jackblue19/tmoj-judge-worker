using Application.Common.Pagination;
using Application.UseCases.Problems.Queries.GetProblemsByUser;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.v2.ProblemManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class UserProblemsController : ControllerBase
{   
    private readonly IMediator _mediator;

    public UserProblemsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    //  GET ALL PERSONAL SUBMITTED PROBLEMS 
    [HttpGet("{userId:guid}/problems")]
    [ProducesResponseType(typeof(ApiPagedResponse<ProblemByUserListItemDto>) , StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiPagedResponse<ProblemByUserListItemDto>>> GetProblemsByUser(
        Guid userId ,
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();

        var isElevated =
            User.IsInRole("admin") ||
            User.IsInRole("manager") ||
            User.IsInRole("teacher");

        var result = await _mediator.Send(
            new GetProblemsByUserQuery(
                UserId: userId ,
                CurrentUserId: currentUserId ,
                IsElevated: isElevated ,
                Page: page ,
                PageSize: pageSize) ,
            ct);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(value , out var userId) ? userId : Guid.Empty;
    }
}