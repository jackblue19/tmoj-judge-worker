using Application.UseCases.Problems.Commands.CreateProblem;
using Application.UseCases.Problems.Commands.CreateTag;
using Application.UseCases.Problems.Commands.UpdateProblem;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Mappings;
using Application.UseCases.Problems.Queries.GetAllProblems;
using Application.UseCases.Problems.Queries.GetProblemById;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v2.ProblemManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//[Authorize]
public class ProblemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    /*
    [HttpGet]
    [OutputCache(
        PolicyName = "ProblemsList" ,
        VaryByQueryKeys = new[] { "difficulty" , "status" , "page" , "pageSize" }
    )]
    public async Task<ActionResult<ApiPagedResponse<ProblemListItemDto>>> GetAll(
        [FromQuery] string? difficulty ,
        [FromQuery] string? status ,
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProblemsQuery(difficulty , status , page , pageSize) ,
            ct);

        var pagination = new PaginationMeta(
            result.Page ,
            result.PageSize ,
            result.TotalCount ,
            (long) Math.Ceiling(result.TotalCount / (double) result.PageSize) ,
            result.Page > 1 ,
            result.Page * result.PageSize < result.TotalCount
        );

        return Ok(ApiPagedResponse<ProblemListItemDto>.Ok(
            result.Items ,
            pagination ,
            traceId: HttpContext.TraceIdentifier));
    }
    */

    //  Create Problem Drafts version 2.1
    /*[HttpPost("drafts")]
    public async Task<ActionResult<ProblemSummaryDto>> CreateDraft(
        [FromBody] CreateProblemDraftRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemDraftCommand(
                request.Title ,
                request.Slug ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.DescriptionMd) ,
            ct);

        return CreatedAtAction(nameof(GetDetail) , new { problemId = result.Id } , result);
    }*/

    //  old version
    /*
    [Authorize]
    [HttpPost("drafts/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<ProblemSummaryDto>> CreateDraftJson(
        [FromBody] CreateProblemDraftRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemDraftCommand(
                request.Title ,
                request.Slug ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.DescriptionMd ,
                null) ,
            ct);

        return CreatedAtAction(nameof(GetDetail) , new { problemId = result.Id } , result);
    }

    [Authorize]
    [HttpPost("drafts/form")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProblemSummaryDto>> CreateDraftForm(
        [FromForm] CreateProblemDraftFormDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemDraftCommand(
                request.Title ,
                request.Slug ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.DescriptionMd ,
                request.StatementFile) ,
            ct);

        return CreatedAtAction(nameof(GetDetail) , new { problemId = result.Id } , result);
    }
    */

    [Authorize]
    [HttpPost("drafts")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProblemSummaryDto>> CreateDraft(
    [FromForm] UpsertProblemContentRequestDto request ,
    CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemDraftCommand(
                request.Title ,
                request.Slug ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.DescriptionMd ,
                request.StatementFile) ,
            ct);

        return CreatedAtAction(nameof(GetDetail) , new { problemId = result.Id } , result);
    }

    [HttpGet("{problemId:guid}")]
    public async Task<ActionResult<ProblemDetailDto>> GetDetail(Guid problemId , CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProblemDetailQuery(problemId) , ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{problemId:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid problemId , CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProblemStatementAccessQuery(problemId) , ct);

        if ( result.Bytes is null || result.Bytes.Length == 0 )
            return NotFound();

        var detail = await _mediator.Send(new GetProblemDetailQuery(problemId) , ct);

        var fileName = !string.IsNullOrWhiteSpace(detail.StatementFileName)
            ? detail.StatementFileName
            : "statement.md";

        return File(
            fileContents: result.Bytes ,
            contentType: result.ContentType ?? "application/octet-stream" ,
            fileDownloadName: fileName ,
            enableRangeProcessing: true);
    }

    //  old-version
    /*
    [HttpPut("{problemId:guid}/content")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProblemDetailDto>> UpdateContentForm(
        Guid problemId ,
        [FromForm] UpdateProblemContentRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProblemContentCommand(
                problemId ,
                request.Title ,
                request.Slug ,
                request.DescriptionMd ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.StatementFile) ,
            ct);

        return Ok(result);
    }
    */

    [Authorize]
    [HttpPut("{problemId:guid}/content")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProblemDetailDto>> UpdateContent(
    Guid problemId ,
    [FromForm] UpsertProblemContentRequestDto request ,
    CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProblemContentCommand(
                problemId ,
                request.Title ,
                request.Slug ,
                request.DescriptionMd ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.TypeCode ,
                request.ScoringCode ,
                request.VisibilityCode ,
                request.StatementFile) ,
            ct);

        return Ok(result);
    }

    [HttpPut("{problemId:guid}/difficulty")]
    public async Task<ActionResult<ProblemDetailDto>> SetDifficulty(
        Guid problemId ,
        [FromBody] SetProblemDifficultyRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new SetProblemDifficultyCommand(problemId , request.Difficulty) ,
            ct);

        return Ok(result);
    }

    [HttpPost("tags")]
    public async Task<ActionResult<ProblemTagDto>> CreateTag(
        [FromBody] CreateTagRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateTagCommand(request.Name , request.Slug) ,
            ct);

        return Ok(result);
    }

    [HttpPost("{problemId:guid}/tags/attach")]
    public async Task<ActionResult<ProblemDetailDto>> AttachTags(
        Guid problemId ,
        [FromBody] AttachProblemTagsRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AttachProblemTagsCommand(problemId , request.TagIds) ,
            ct);

        return Ok(result);
    }

    [HttpPut("{problemId:guid}/tags")]
    public async Task<ActionResult<ProblemDetailDto>> ReplaceTags(
        Guid problemId ,
        [FromBody] ReplaceProblemTagsRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ReplaceProblemTagsCommand(problemId , request.TagIds) ,
            ct);

        return Ok(result);
    }
}
