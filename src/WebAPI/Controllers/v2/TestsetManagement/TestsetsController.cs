using Application.UseCases.Testsets.Commands;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
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

    [RequestSizeLimit(200 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200 * 1024 * 1024)]
    [HttpPost("{id:guid}/testcases")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
}
