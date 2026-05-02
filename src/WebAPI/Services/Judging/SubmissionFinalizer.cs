using Contracts.Submissions.Judging;
using Domain.Entities;

namespace WebAPI.Services.Judging;

public static class SubmissionFinalizer
{
    public static void ApplySubmissionSummary(
        Submission submission ,
        JudgeSummaryResultContract summary ,
        int? resolvedTimeMs ,
        int? resolvedMemoryKb)
    {
        submission.StatusCode = "done";
        submission.VerdictCode = ResultStatusMapper.NormalizeVerdict(summary.Verdict);
        submission.TimeMs = resolvedTimeMs;
        submission.MemoryKb = resolvedMemoryKb;
        submission.FinalScore = summary.FinalScore ?? 0;
        submission.JudgedAt = DateTime.UtcNow;
    }

    public static void ApplySubmissionSummary(
        Submission submission ,
        JudgeSummaryResultContract summary)
    {
        ApplySubmissionSummary(
            submission ,
            summary ,
            summary.TimeMs ,
            summary.MemoryKb);
    }
}