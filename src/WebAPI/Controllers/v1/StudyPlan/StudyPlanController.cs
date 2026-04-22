using Application.Common.Pagination;
using Application.UseCases.Problems.Commands.CreateProblem;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Queries.GetPublicProblems;
using Application.UseCases.StudyPlans.Commands.AddProblemToPlan;
using Application.UseCases.StudyPlans.Commands.BuyStudyPlan;
using Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using Application.UseCases.StudyPlans.Commands.EnrollStudyPlan;
using Application.UseCases.StudyPlans.Dtos;
using Application.UseCases.StudyPlans.Queries.GetNextStudyPlanItem;
using Application.UseCases.StudyPlans.Queries.GetStudyPlanDetail;
using Application.UseCases.StudyPlans.Queries.GetStudyPlanEnrollment;
using Application.UseCases.StudyPlans.Queries.GetStudyPlans;
using Application.UseCases.StudyPlans.Queries.GetStudyPlanStats;
using Application.UseCases.StudyPlans.Queries.GetUnlockedPlans;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.StudyPlans;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/study-plans")]
public class StudyPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudyPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =========================
    // CREATE PLAN
    // =========================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudyPlanCommand command,
        CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(command, ct);

            return Ok(ApiResponse<object>.Ok(
                new { studyPlanId = id },
                "Study plan created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // ADD PROBLEM TO PLAN
    // =========================
    [HttpPost("{planId:guid}/problems/{problemId:guid}")]
    [Authorize]
    public async Task<IActionResult> AddProblem(
        Guid planId,
        Guid problemId,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AddProblemToPlanCommand
            {
                StudyPlanId = planId,
                ProblemId = problemId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "Problem added to study plan"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // 🔥 BUY STUDY PLAN (COIN FLOW)
    // =========================
    [HttpPost("{planId:guid}/buy")]
    [Authorize]
    public async Task<IActionResult> Buy(Guid planId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new BuyStudyPlanCommand
            {
                StudyPlanId = planId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "Buy success (coin deducted / access granted)"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // ENROLL (ONLY CREATE PROGRESS)
    // =========================
    [HttpPost("{planId:guid}/enroll")]
    [Authorize]
    public async Task<IActionResult> Enroll(Guid planId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EnrollStudyPlanCommand
            {
                StudyPlanId = planId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                true,
                "Enrolled successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // UNLOCKED LIST
    // =========================
    [HttpGet("unlocked")]
    [Authorize]
    public async Task<IActionResult> GetUnlocked(CancellationToken ct)
    {
        try
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            var result = await _mediator.Send(new GetUnlockedPlansQuery
            {
                UserId = userId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched unlocked plans"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // DETAIL
    // =========================
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetStudyPlanDetailQuery
            {
                StudyPlanId = id
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched study plan detail"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // NEXT ITEM
    // =========================
    [HttpGet("{planId:guid}/next/{itemId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetNext(
        Guid planId,
        Guid itemId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetNextStudyPlanItemQuery
            {
                StudyPlanId = planId,
                StudyPlanItemId = itemId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched next item"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // ENROLLMENT STATUS
    // =========================
    [HttpGet("{planId:guid}/enrollment")]
    [Authorize]
    public async Task<IActionResult> GetEnrollment(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetStudyPlanEnrollmentQuery
            {
                StudyPlanId = planId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched enrollment info"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // STATS
    // =========================
    [HttpGet("{planId:guid}/stats")]
    [Authorize]
    public async Task<IActionResult> GetStats(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetStudyPlanStatsQuery
            {
                StudyPlanId = planId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched stats"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // LIST STUDY PLANS
    // =========================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] Guid? creatorId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetStudyPlansQuery
            {
                CreatorId = creatorId
            }, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Fetched study plans"));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================
    // CREATE PROBLEM IN PLAN
    // =========================
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("/problem/in-plan")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> Create(
       [FromForm] UpsertProblemContentRequestDto request,
       CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemCommand(
                request.Title,
                request.Slug,
                request.Difficulty,
                request.TypeCode,
                "in-plan",
                 request.ScoringCode,
                request.StatusCode,
                request.TimeLimitMs,
                request.MemoryLimitKb,
                request.DescriptionMd,
                request.StatementFile,
                request.TagIds),
            ct);

        return CreatedAtAction(
            nameof(GetDetail),
            new { problemId = result.Id },
            ApiResponse<ProblemDetailDto>.Ok(
                result,
                "Problem created successfully.",
                HttpContext.TraceIdentifier));
    }
}