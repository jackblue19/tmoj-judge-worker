using Application.Common.Pagination;
using Application.UseCases.Reports.Commands;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Queries;
using Application.UseCases.Users.Commands;
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

    // =====================================================
    // CREATE REPORT
    // POST: /api/v1/reports
    // =====================================================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReport(
        [FromBody] CreateReportCommand command,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid request" });

        try
        {
            var reportId = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(new
            {
                reportId
            }, "Report created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // APPROVE REPORT
    // POST: /api/v1/reports/{id}/approve
    // =====================================================
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ApproveReport(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ApproveReportCommand(id), ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "Report approved successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // REJECT REPORT
    // POST: /api/v1/reports/{id}/reject
    // =====================================================
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> RejectReport(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RejectReportCommand(id), ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "Report rejected successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // GET PENDING REPORTS (Cursor Pagination)
    // GET: /api/v1/reports/pending
    // =====================================================
    [HttpGet("pending")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetPendingReports(
        [FromQuery] DateTime? cursorCreatedAt,
        [FromQuery] Guid? cursorId,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (pageSize <= 0 || pageSize > 50)
            return BadRequest(new { message = "pageSize must be between 1 and 50" });

        try
        {
            var result = await _mediator.Send(
                new GetPendingReportsQuery(cursorCreatedAt, cursorId, pageSize),
                ct);

            return Ok(ApiResponse<CursorPaginationDto<ReportDto>>.Ok(
                result,
                "Fetched pending reports successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // GET MY REPORTS
    // GET: /api/v1/reports/my
    // =====================================================
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReports(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetMyReportsQuery(), ct);

            return Ok(ApiResponse<List<ReportDto>>.Ok(
                result,
                "Fetched my reports successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // GET ALL REPORTS (FILTER BY STATUS)
    // GET: /api/v1/reports?status=pending
    // =====================================================
    [HttpGet]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetReportsQuery(status), ct);

            return Ok(ApiResponse<List<ReportDto>>.Ok(
                result,
                "Fetched reports successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // GET REPORT BY ID
    // GET: /api/v1/reports/{id}
    // =====================================================
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetReportById(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetReportByIdQuery(id), ct);

            return Ok(ApiResponse<ReportDto>.Ok(
                result,
                "Fetched report successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =====================================================
    // UNBAN USER 
    // POST: /api/v1/reports/users/{id}/unban
    // =====================================================
    [HttpPost("users/{id:guid}/unban")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> UnbanUser(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UnbanUserCommand(id), ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "User unbanned successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    // =====================================================
    // GET REPORT GROUPS
    // GET: /api/v1/reports/groups
    // =====================================================
    [HttpGet("groups")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetReportGroups(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GetReportGroupsQuery(status), ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched report groups successfully"
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}