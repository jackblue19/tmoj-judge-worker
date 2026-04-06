using Application.Common.Pagination;
using Application.UseCases.Reports.Commands;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Reports;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================================
    // POST: /api/v1/reports
    // =========================================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReport(
        [FromBody] CreateReportCommand command,
        CancellationToken ct)
    {
        var reportId = await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            reportId
        }, "Report created successfully"));
    }

    // =========================================
    // POST: /api/v1/reports/{id}/approve
    // =========================================
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ApproveReport(
        Guid id,
        CancellationToken ct)
    {
        await _mediator.Send(new ApproveReportCommand(id), ct);

        return Ok(ApiResponse<object>.Ok(true, "Report approved successfully"));
    }

    // =========================================
    // GET: /api/v1/reports/pending
    // =========================================
    [HttpGet("pending")]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<CursorPaginationDto<ReportDto>>> GetPendingReports(
        [FromQuery] DateTime? cursorCreatedAt,
        [FromQuery] Guid? cursorId,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (pageSize <= 0 || pageSize > 50)
            return BadRequest(new { Message = "pageSize must be between 1 and 50" });

        var result = await _mediator.Send(
            new GetPendingReportsQuery(cursorCreatedAt, cursorId, pageSize),
            ct);

        return Ok(ApiResponse<CursorPaginationDto<ReportDto>>
            .Ok(result, "Fetched pending reports successfully"));
    }

    // =========================================
    // POST: /api/v1/reports/{id}/reject
    // =========================================

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> RejectReport(
    Guid id,
    CancellationToken ct)
    {
        await _mediator.Send(new RejectReportCommand(id), ct);

        return Ok(ApiResponse<object>.Ok(true, "Report rejected successfully"));
    }
}