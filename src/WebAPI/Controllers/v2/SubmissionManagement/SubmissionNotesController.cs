using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.Submissions;
using WebAPI.Services.Judging;

namespace WebAPI.Controllers.v2.SubmissionManagement;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/submissions")]
[Authorize]
public sealed class SubmissionNotesController : ControllerBase
{
    private readonly SubmissionNoteService _noteService;

    public SubmissionNotesController(SubmissionNoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPatch("{submissionId:guid}/note")]
    public async Task<IActionResult> UpdateSubmissionNote(
        Guid submissionId ,
        [FromBody] UpdateSubmissionNoteRequest req ,
        CancellationToken ct)
    {
        if ( !CanReviewNotes() )
            return Forbid();

        var ok = await _noteService.UpdateSubmissionNoteAsync(
            submissionId ,
            req.Note ,
            ct);

        if ( !ok )
            return NotFound();

        return Ok(new
        {
            ok = true ,
            submissionId ,
            note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim()
        });
    }

    [HttpPatch("results/{resultId:guid}/note")]
    public async Task<IActionResult> UpdateResultNote(
        Guid resultId ,
        [FromBody] UpdateResultNoteRequest req ,
        CancellationToken ct)
    {
        if ( !CanReviewNotes() )
            return Forbid();

        var ok = await _noteService.UpdateResultNoteAsync(
            resultId ,
            req.Note ,
            ct);

        if ( !ok )
            return NotFound();

        return Ok(new
        {
            ok = true ,
            resultId ,
            note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim()
        });
    }

    private bool CanReviewNotes()
    {
        return User.IsInRole("admin")
            || User.IsInRole("manager")
            || User.IsInRole("teacher");
    }
}