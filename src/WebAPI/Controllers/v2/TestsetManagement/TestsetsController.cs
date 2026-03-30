using Application.UseCases.Testsets.Commands;
using Application.UseCases.Testsets.Queries;
using Asp.Versioning;
using Infrastructure.Configurations.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v2.TestsetManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TestsetsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    public TestsetsController(IMediator mediator , IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("{id:guid}/testcases")]
    public async Task<IActionResult> UploadTestcasesZip(
        Guid id ,
        [FromForm] UploadTestcasesFormDto form ,
        CancellationToken ct)
    {
        if ( form.File is null || form.File.Length == 0 )
            return BadRequest("File is required.");

        await using var stream = form.File.OpenReadStream();

        var result = await _mediator.Send(
            new UploadTestcasesZipCommand(
                id ,
                form.TestsetId ,
                form.ReplaceExisting ,
                form.File.FileName ,
                stream) ,
            ct);

        return Ok(result);
    }

    //  Preview 3 testcase -> chuyển qua dùng /samples
    //[NonAction]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("{problemId:guid}/{testsetId:guid}/preview")]
    public async Task<IActionResult> GetPreview(
        Guid problemId ,
        Guid testsetId ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTestsetPreviewQuery(problemId , testsetId) ,
            ct);

        return Ok(result);
    }

    [HttpGet("{problemId:guid}/{testsetId:guid}/samples")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSamples(
    Guid problemId ,
    Guid testsetId ,
    CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetSampleTestcasesQuery(problemId , testsetId) ,
            ct);

        return Ok(result);
    }

    //  Get all testcase
    //[NonAction]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin")]
    [HttpGet("{problemId:guid}/{testsetId:guid}/all")]
    public async Task<IActionResult> GetAll(
        Guid problemId ,
        Guid testsetId ,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAllTestcasesQuery(problemId , testsetId) ,
            ct);

        return Ok(result);
    }

    [HttpGet("{problemId:guid}/{testsetId:guid}/download-zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize] // enable auth
    public async Task<IActionResult> DownloadZip(
    Guid problemId ,
    Guid testsetId ,
    CancellationToken ct)
    {
        var isInternal = InternalAuthHelper.IsInternalRequest(HttpContext);
        var hasApiKey = InternalAuthHelper.HasValidApiKey(HttpContext , _configuration);
        var isAdmin = User.IsInRole("Admin");

        if ( !isInternal && !hasApiKey && !isAdmin )
            return Unauthorized("Invalid access");

        HttpContext.Items["IsInternal"] = isInternal || hasApiKey;

        var result = await _mediator.Send(
            new DownloadTestsetZipQuery(problemId , testsetId) ,
            ct);

        return File(result.Bytes , result.ContentType , result.FileName);
    }
}