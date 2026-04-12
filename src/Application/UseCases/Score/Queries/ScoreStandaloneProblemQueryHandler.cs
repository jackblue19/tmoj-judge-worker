using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreStandaloneProblemQueryHandler
    : IRequestHandler<ScoreStandaloneProblemQuery, ScoreStandaloneProblemQueryResult>
{
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<Testcase, Guid> _testcaseRepo;

    public ScoreStandaloneProblemQueryHandler(
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<Testcase, Guid> testcaseRepo)
    {
        _submissionRepo = submissionRepo;
        _resultRepo = resultRepo;
        _testcaseRepo = testcaseRepo;
    }

    public async Task<ScoreStandaloneProblemQueryResult> Handle(
        ScoreStandaloneProblemQuery request,
        CancellationToken ct)
    {
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null)
            return new ScoreStandaloneProblemQueryResult(NotFound: true, BelongsToContest: false, Data: null);

        if (submission.ContestProblemId.HasValue)
            return new ScoreStandaloneProblemQueryResult(NotFound: false, BelongsToContest: true, Data: null);

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

        var dto = new IoiStandaloneProblemScoreDto(
            SubmissionId: request.SubmissionId,
            ProblemId: submission.ProblemId,
            ScoringMode: "ioi",
            TotalScore: totalScore,
            PassedCases: passedCases,
            TotalCases: totalCases,
            Cases: cases);

        return new ScoreStandaloneProblemQueryResult(NotFound: false, BelongsToContest: false, Data: dto);
    }
}
