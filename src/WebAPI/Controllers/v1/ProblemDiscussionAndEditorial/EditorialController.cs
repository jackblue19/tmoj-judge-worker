using Application.UseCases.Editorials.Commands;
using Application.UseCases.Editorials.Dtos;
using Application.UseCases.Editorials.Queries;
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

    /// GET LIST
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid problemId,
        [FromQuery] Guid? cursorId,
        [FromQuery] DateTime? cursorCreatedAt,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (problemId == Guid.Empty)
            return BadRequest(new { Message = "problemId is required" });

        if (pageSize <= 0 || pageSize > 50)
            return BadRequest(new { Message = "pageSize must be between 1 and 50" });

        var result = await _mediator.Send(new ViewEditorialQuery
        {
            ProblemId = problemId,
            CursorId = cursorId,
            CursorCreatedAt = cursorCreatedAt,
            PageSize = pageSize
        }, ct);

        return Ok(ApiResponse<object>.Ok(result, "Fetched editorials successfully"));
    }

    /// CREATE
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
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEditorial(Guid id, UpdateEditorialCommand command)
    {
        command.EditorialId = id;

        var result = await _mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(result, "Editorial updated"));
    }

    /// DELETE
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var cmd = new DeleteEditorialCommand
        {
            EditorialId = id
        };

        await _mediator.Send(cmd, ct);

        return Ok(ApiResponse<object>.Ok(new {}, "Editorial deleted"));
    }

    /// GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEditorialByIdQuery
        {
            EditorialId = id
        }, ct);

        if (result == null)
            return NotFound(new { Message = "Editorial not found" });

        return Ok(ApiResponse<object>.Ok(result, "Fetched editorial successfully"));
    }
}