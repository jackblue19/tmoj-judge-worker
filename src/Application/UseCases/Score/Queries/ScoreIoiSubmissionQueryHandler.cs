using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreIoiSubmissionQueryHandler
    : IRequestHandler<ScoreIoiSubmissionQuery, IoiSubmissionScoreDto?>
{
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<Testcase, Guid> _testcaseRepo;

    public ScoreIoiSubmissionQueryHandler(
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<Testcase, Guid> testcaseRepo)
    {
        _submissionRepo = submissionRepo;
        _resultRepo = resultRepo;
        _testcaseRepo = testcaseRepo;
    }

    public async Task<IoiSubmissionScoreDto?> Handle(
        ScoreIoiSubmissionQuery request,
        CancellationToken ct)
    {
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return null;

        var results = await _resultRepo.ListAsync(new ResultsBySubmissionSpec(request.SubmissionId), ct);

        var testcaseIds = results
            .Where(r => r.TestcaseId.HasValue)
            .Select(r => r.TestcaseId!.Value)
            .Distinct()
            .ToList();

        var ordinalMap = testcaseIds.Count == 0
            ? new Dictionary<Guid, int>()
            : (await _testcaseRepo.ListAsync(new TestcasesByIdsSpec(testcaseIds), ct))
                .ToDictionary(t => t.Id, t => t.Ordinal);

        var (totalScore, passedCases, totalCases, cases) =
            ScoringHelper.CalcIoiScore(results, ordinalMap);

        return new IoiSubmissionScoreDto(
            SubmissionId: request.SubmissionId,
            ScoringMode: "ioi",
            Verdict: submission.VerdictCode,
            Note: submission.VerdictCode == "ce"
                ? "Submission bị Compile Error, không có test case nào được chạy."
                : null,
            TotalScore: totalScore,
            PassedCases: passedCases,
            TotalCases: totalCases,
            Cases: cases);
    }
}
