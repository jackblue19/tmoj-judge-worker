using Application.UseCases.Score.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/debug/score")]
[Tags("ScoreDebug")]
public class ScoreDebugController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScoreDebugController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =============================================
    // IOI — per submission
    // =============================================
    [HttpGet("ioi/submission/{submissionId:guid}")]
    public async Task<IActionResult> ScoreIoiSubmission(Guid submissionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreIoiSubmissionQuery(submissionId), ct);
        return result is null ? NotFound(new { message = "Submission not found." }) : Ok(result);
    }

    // =============================================
    // IOI — per problem (best score qua nhiều lần nộp)
    // =============================================
    [HttpGet("ioi/problem/{contestProblemId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreIoiProblem(Guid contestProblemId, Guid teamId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreIoiProblemQuery(contestProblemId, teamId), ct);
        return result is null ? NotFound(new { message = "ContestProblem not found." }) : Ok(result);
    }

    // =============================================
    // CONTEST — auto detect IOI/ACM theo Contest.ContestType
    // =============================================
    [HttpGet("contest/{contestId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreContest(Guid contestId, Guid teamId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreContestQuery(contestId, teamId), ct);
        return result is null ? NotFound(new { message = "Contest not found." }) : Ok(result);
    }

    // =============================================
    // STANDALONE PROBLEM — luôn IOI
    // =============================================
    [HttpGet("problem/{submissionId:guid}")]
    public async Task<IActionResult> ScoreStandaloneProblem(Guid submissionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreStandaloneProblemQuery(submissionId), ct);

        if (result.NotFound)
            return NotFound(new { message = "Submission not found." });

        if (result.BelongsToContest)
            return BadRequest(new { message = "Submission belongs to a contest. Use /contest/{contestId}/{teamId} instead." });

        return Ok(result.Data);
    }

    // =============================================
    // ACM — per problem
    // =============================================
    [HttpGet("acm/problem/{contestProblemId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreAcmProblem(Guid contestProblemId, Guid teamId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreAcmProblemQuery(contestProblemId, teamId), ct);
        return result is null ? NotFound(new { message = "ContestProblem not found." }) : Ok(result);
    }

    // =============================================
    // ACM — toàn contest
    // =============================================
    [HttpGet("acm/contest/{contestId:guid}/{teamId:guid}")]
    public async Task<IActionResult> ScoreAcmContest(Guid contestId, Guid teamId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ScoreAcmContestQuery(contestId, teamId), ct);
        return result is null ? NotFound(new { message = "Contest not found." }) : Ok(result);
    }

    // =============================================
    // DEBUG — raw data của submission
    // =============================================
    [HttpGet("inspect/{submissionId:guid}")]
    public async Task<IActionResult> InspectSubmission(Guid submissionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new InspectSubmissionQuery(submissionId), ct);
        return result is null ? NotFound(new { message = "Submission not found." }) : Ok(result);
    }
}
