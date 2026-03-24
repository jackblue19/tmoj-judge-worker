using Application.UseCases.Editorials;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EditorialController : ControllerBase
{
    private readonly IMediator _mediator;

    public EditorialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get editorials by problemId (cursor pagination)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid problemId,
        [FromQuery] Guid? cursorId,
        [FromQuery] DateTime? cursorCreatedAt,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        // 🔥 Validate
        if (problemId == Guid.Empty)
            return BadRequest(new { Message = "problemId is required" });

        if (pageSize <= 0 || pageSize > 50)
            return BadRequest(new { Message = "pageSize must be between 1 and 50" });

        var result = await _mediator.Send(
            new ViewEditorialQuery(
                problemId,
                cursorId,
                cursorCreatedAt,
                pageSize
            ),
            ct
        );

        return Ok(ApiResponse<object>.Ok(result, "Fetched editorials successfully"));
    }

    /// <summary>
    /// Create editorial (Admin / Manager only)
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEditorialCommand cmd,
        CancellationToken ct)
    {
        if (cmd == null)
            return BadRequest(new { Message = "Invalid request" });

        var id = await _mediator.Send(cmd, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            EditorialId = id
        }, "Editorial created successfully"));
    }
}