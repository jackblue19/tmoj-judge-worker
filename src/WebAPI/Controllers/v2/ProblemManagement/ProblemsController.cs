using Application.Common.Pagination;
using Application.UseCases.Problems.Commands.AttachProblemTags;
using Application.UseCases.Problems.Commands.CreateProblem;
using Application.UseCases.Problems.Commands.CreateRemixProblem;
using Application.UseCases.Problems.Commands.CreateTag;
using Application.UseCases.Problems.Commands.CreateVirtualProblem;
using Application.UseCases.Problems.Commands.DonateProblem;
using Application.UseCases.Problems.Commands.ReplaceProblemTags;
using Application.UseCases.Problems.Commands.UpdateProblem;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Queries.GetAllTags;
using Application.UseCases.Problems.Queries.GetProblemBanks;
using Application.UseCases.Problems.Queries.GetProblemById;
using Application.UseCases.Problems.Queries.GetPublicProblems;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v2.ProblemManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProblemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProblemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // CREATE PROBLEM
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> Create(
        [FromForm] UpsertProblemContentRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemCommand(
                request.Title ,
                request.Slug ,
                request.Difficulty ,
                request.TypeCode ,
                request.ScoringCode ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.DescriptionMd ,
                request.StatementFile ,
                request.TagIds ,
                request.ProblemMode) ,
            ct);

        return CreatedAtAction(
            nameof(GetDetail) ,
            new { problemId = result.Id } ,
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem created successfully." ,
                HttpContext.TraceIdentifier));
    }

    //  DOANTE PROBLEM -> VISIBILITY CODE = IN-BANK
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("donate")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> Donate(
        [FromForm] UpsertProblemContentRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new DonateProblemCommand(
                request.Title ,
                request.Slug ,
                request.Difficulty ,
                request.TypeCode ,
                request.ScoringCode ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.DescriptionMd ,
                request.StatementFile ,
                request.TagIds ,
                request.ProblemMode) ,
            ct);

        return CreatedAtAction(
            nameof(GetDetail) ,
            new { problemId = result.Id } ,
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem donated successfully." ,
                HttpContext.TraceIdentifier));
    }


    //  virtual problem
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("virtual")]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> CreateVirtual(
        [FromBody] CreateVirtualProblemRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
         new CreateVirtualProblemCommand(
             request.OriginProblemId ,
             request.OriginProblemSlug ,
             request.Slug ,
             request.Title ,
             request.VisibilityCode) ,
         ct);

        return CreatedAtAction(
            nameof(GetDetail) ,
            new { problemId = result.Id } ,
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Virtual problem created successfully." ,
                HttpContext.TraceIdentifier));
    }

    //  REMIX PROBLEM
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("remix")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> CreateRemix(
        [FromForm] CreateRemixProblemRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateRemixProblemCommand(
                request.OriginProblemId ,
                request.OriginProblemSlug ,
                request.Title ,
                request.Slug ,
                request.Difficulty ,
                request.TypeCode ,
                request.VisibilityCode ,
                request.ScoringCode ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.DescriptionMd ,
                request.StatementFile ,
                request.TagIds ,
                request.ProblemMode) ,
            ct);

        return CreatedAtAction(
            nameof(GetDetail) ,
            new { problemId = result.Id } ,
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Remix problem created successfully." ,
                HttpContext.TraceIdentifier));
    }

    // GET PROBLEM DETAIL
    [AllowAnonymous]
    [HttpGet("{problemId:guid}")]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> GetDetail(Guid problemId , CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProblemDetailQuery(problemId) , ct);

        return Ok(
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem detail fetched successfully." ,
                HttpContext.TraceIdentifier));
    }

    // GET PROBLEM STATEMENT
    [Authorize(Roles = "admin,manager,teacher")]
    //[Authorize]
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

    // UPDATE PROBLEM
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{problemId:guid}/content")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> UpdateContent(
        Guid problemId ,
        [FromForm] UpsertProblemContentRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateProblemContentCommand(
                problemId ,
                request.Title ,
                request.Slug ,
                request.Difficulty ,
                request.TypeCode ,
                request.ScoringCode ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.DescriptionMd ,
                request.StatementFile ,
                request.TagIds ,
                request.ProblemMode) ,
            ct);

        return Ok(
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem updated successfully." ,
                HttpContext.TraceIdentifier));
    }

    // CREATE TAG
    [Authorize(Roles = "admin,manager,teacher")]
    //[Authorize]
    [HttpPost("tags")]
    public async Task<ActionResult<ApiResponse<ProblemTagDto>>> CreateTag(
        [FromBody] CreateTagRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateTagCommand(
                request.Name ,
                request.Slug ,
                request.Description ,
                request.Color ,
                request.Icon) ,
            ct);

        return Ok(
            ApiResponse<ProblemTagDto>.Ok(
                result ,
                "Tag created successfully." ,
                HttpContext.TraceIdentifier));
    }

    // GET ALL TAGS
    [HttpGet("tags")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AllTagsListItemDto>>>> GetAllTags(
        [FromQuery] bool includeInactive = false ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAllTagsQuery(includeInactive) ,
            ct);

        return Ok(
            ApiResponse<IReadOnlyList<AllTagsListItemDto>>.Ok(
                result ,
                "Tags fetched successfully." ,
                HttpContext.TraceIdentifier));
    }

    // ATTACH TAGS TO PROBLEM
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPost("{problemId:guid}/tags/attach")]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> AttachTags(
        Guid problemId ,
        [FromBody] AttachProblemTagsRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new AttachProblemTagsCommand(problemId , request.TagIds) ,
            ct);

        return Ok(
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Tags attached successfully." ,
                HttpContext.TraceIdentifier));
    }

    // REPLACE PROBLEM TAGS
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpPut("{problemId:guid}/tags")]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> ReplaceTags(
        Guid problemId ,
        [FromBody] ReplaceProblemTagsRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ReplaceProblemTagsCommand(problemId , request.TagIds) ,
            ct);

        return Ok(
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem tags replaced successfully." ,
                HttpContext.TraceIdentifier));
    }

    // GET ALL PUBLIC PROBLEMS
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiPagedResponse<PublicProblemListItemDto>) , StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiPagedResponse<PublicProblemListItemDto>>> GetPublicProblems(
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        [FromQuery] string? search = null ,
        [FromQuery] string? difficulty = null ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPublicProblemsQuery(
                Page: page ,
                PageSize: pageSize ,
                Search: search ,
                Difficulty: difficulty) ,
            ct);

        return Ok(result);
    }

    //  GET PROBLEM BANKS (IN-BANK)
    // GET PROBLEM BANKS
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("banks")]
    [ProducesResponseType(typeof(ApiPagedResponse<ProblemBankListItemDto>) , StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiPagedResponse<ProblemBankListItemDto>>> GetProblemBanks(
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        [FromQuery] string? search = null ,
        [FromQuery] string? difficulty = null ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProblemBanksQuery(
                Page: page ,
                PageSize: pageSize ,
                Search: search ,
                Difficulty: difficulty) ,
            ct);

        return Ok(result);
    }

    // GET IN-PLAN PROBLEMS
    [Authorize(Roles = "admin,manager,teacher")]
    [HttpGet("in-plan")]
    [ProducesResponseType(typeof(ApiPagedResponse<ProblemBankListItemDto>) , StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiPagedResponse<ProblemBankListItemDto>>> GetInPlanProblems(
        [FromQuery] int page = 1 ,
        [FromQuery] int pageSize = 20 ,
        [FromQuery] string? search = null ,
        [FromQuery] string? difficulty = null ,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new Application.UseCases.Problems.Queries.GetInPlanProblems.GetInPlanProblemsQuery(
                Page: page ,
                PageSize: pageSize ,
                Search: search ,
                Difficulty: difficulty) ,
            ct);

        return Ok(result);
    }

    // UPDATE PROBLEM DIFFICULTY
    [Authorize(Roles = "admin,manager,teacher")]
    //[Authorize]
    [HttpPut("{problemId:guid}/difficulty")]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> SetDifficulty(
        Guid problemId ,
        [FromBody] SetProblemDifficultyRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new SetProblemDifficultyCommand(problemId , request.Difficulty) ,
            ct);

        return Ok(
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem difficulty updated successfully." ,
                HttpContext.TraceIdentifier));
    }

    //  create draft => for student
    /*[Authorize]
    [HttpPost("drafts")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ProblemDetailDto>>> CreateDraft(
        [FromForm] UpsertProblemContentRequestDto request ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProblemCommand(
                request.Title ,
                request.Slug ,
                request.Difficulty ,
                request.TypeCode ,
                request.VisibilityCode ,
                request.ScoringCode ,
                "draft" ,
                request.TimeLimitMs ,
                request.MemoryLimitKb ,
                request.DescriptionMd ,
                request.StatementFile ,
                request.TagIds) ,
            ct);

        return CreatedAtAction(
            nameof(GetDetail) ,
            new { problemId = result.Id } ,
            ApiResponse<ProblemDetailDto>.Ok(
                result ,
                "Problem draft created successfully." ,
                HttpContext.TraceIdentifier));
    }*/
}