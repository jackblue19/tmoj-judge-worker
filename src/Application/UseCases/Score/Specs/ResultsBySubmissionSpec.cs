using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

/// <summary>
/// Lấy Result của submission có gắn TestcaseId (loại bỏ row compile).
/// </summary>
public sealed class ResultsBySubmissionSpec : Specification<Result>
{
    public ResultsBySubmissionSpec(Guid submissionId)
    {
        Query.Where(r => r.SubmissionId == submissionId && r.TestcaseId != null);
    }
}

/// <summary>
/// Lấy tất cả Result của submission, kể cả row compile (dùng cho debug/inspect).
/// </summary>
public sealed class AllResultsBySubmissionSpec : Specification<Result>
{
    public AllResultsBySubmissionSpec(Guid submissionId)
    {
        Query.Where(r => r.SubmissionId == submissionId);
    }
}

/// <summary>
/// Batch-load Result (có TestcaseId) cho nhiều submission — tránh N+1 khi tính
/// leaderboard IOI cho toàn contest.
/// </summary>
public sealed class ResultsBySubmissionIdsSpec : Specification<Result>
{
    public ResultsBySubmissionIdsSpec(IReadOnlyCollection<Guid> submissionIds)
    {
        Query.Where(r => submissionIds.Contains(r.SubmissionId) && r.TestcaseId != null);
    }
}
