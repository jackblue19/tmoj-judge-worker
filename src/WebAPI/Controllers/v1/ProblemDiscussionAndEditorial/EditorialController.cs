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

    /// UPDATE
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEditorialCommand body,
        CancellationToken ct)
    {
        var cmd = body with { EditorialId = id };

        await _mediator.Send(cmd, ct);

        return Ok(ApiResponse<object>.Ok(new {}, "Editorial updated"));
    }

    /// DELETE
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteEditorialCommand(id), ct);

        return Ok(ApiResponse<object>.Ok(new {}, "Editorial deleted"));
    }

    /// GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEditorialByIdQuery(id), ct);

        if (result == null)
            return NotFound(new { Message = "Editorial not found" });
       
        return Ok(ApiResponse<object>.Ok(result, "Fetched editorial successfully"));
    }
}