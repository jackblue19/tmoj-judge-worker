using System.Security.Claims;
using Application.UseCases.AI;
using Application.UseCases.AI.Dtos;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.v2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly ISender _sender;

    public AiController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("submissions/{submissionId:guid}/ai/debug")]
    public async Task<IActionResult> GenerateDebug(
        Guid submissionId ,
        [FromBody] GenerateAiDebugRequestDto request ,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await _sender.Send(new GenerateAiDebugCommand(
            SubmissionId: submissionId ,
            ResultId: request.ResultId ,
            CurrentUserId: userId ,
            LanguageCode: request.LanguageCode) , ct);

        return Ok(new
        {
            succeeded = true ,
            code = "ai.debug.generated" ,
            message = "AI debug explanation generated successfully." ,
            data = result
        });
    }

    [HttpPost("problems/{problemId:guid}/ai/editorial-drafts")]
    public async Task<IActionResult> GenerateEditorialDraft(
        Guid problemId ,
        [FromBody] GenerateAiEditorialDraftRequestDto request ,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await _sender.Send(new GenerateAiEditorialDraftCommand(
            ProblemId: problemId ,
            CurrentUserId: userId ,
            LanguageCode: request.LanguageCode ,
            StyleCode: request.StyleCode ,
            TargetAudienceCode: request.TargetAudienceCode ,
            IncludePseudocode: request.IncludePseudocode ,
            IncludeCorrectness: request.IncludeCorrectness ,
            IncludeComplexity: request.IncludeComplexity) , ct);

        return Ok(new
        {
            succeeded = true ,
            code = "ai.editorial_draft.generated" ,
            message = "AI editorial draft generated successfully." ,
            data = result
        });
    }

    private Guid GetCurrentUserId()
    {
        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        if ( !Guid.TryParse(raw , out var userId) )
            throw new UnauthorizedAccessException("Invalid user token.");

        return userId;
    }
}