using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Judging;

public sealed class SubmissionNoteService
{
    private readonly TmojDbContext _db;

    public SubmissionNoteService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> UpdateSubmissionNoteAsync(
        Guid submissionId ,
        string? note ,
        CancellationToken ct)
    {
        var submission = await _db.Submissions
            .FirstOrDefaultAsync(x => x.Id == submissionId , ct);

        if ( submission is null )
            return false;

        submission.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateResultNoteAsync(
        Guid resultId ,
        string? note ,
        CancellationToken ct)
    {
        var result = await _db.Results
            .FirstOrDefaultAsync(x => x.Id == resultId , ct);

        if ( result is null )
            return false;

        result.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        await _db.SaveChangesAsync(ct);
        return true;
    }
}