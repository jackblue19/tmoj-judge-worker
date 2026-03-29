using Application.UseCases.Testsets.Commands;
using Application.UseCases.Testsets.Queries;
using Asp.Versioning;
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

    public TestsetsController(IMediator mediator)
    {
        _mediator = mediator;
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

    // 🔹 Preview 3 testcase
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

    // 🔹 Get all testcase
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
    public async Task<IActionResult> DownloadZip(
    Guid problemId ,
    Guid testsetId ,
    CancellationToken ct)
    {
        var result = await _mediator.Send(
            new DownloadTestsetZipQuery(problemId , testsetId) ,
            ct);

        return File(result.Bytes , result.ContentType , result.FileName);
    }
}