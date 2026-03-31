using Contracts.Submissions.Judging;
using Domain.Entities;

namespace WebAPI.Services.Judging;

public static class SubmissionFinalizer
{
    public static void ApplySubmissionSummary(
        Submission submission ,
        JudgeSummaryResultContract summary)
    {
        submission.StatusCode = "done";
        submission.VerdictCode = ResultStatusMapper.NormalizeVerdict(summary.Verdict);
        submission.TimeMs = summary.TimeMs;
        submission.MemoryKb = summary.MemoryKb;
        submission.FinalScore = summary.FinalScore;
        submission.JudgedAt = DateTime.UtcNow;
    }
}