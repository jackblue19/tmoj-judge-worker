using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreIoiProblemQueryHandler
    : IRequestHandler<ScoreIoiProblemQuery, IoiProblemScoreDto?>
{
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<Testcase, Guid> _testcaseRepo;

    public ScoreIoiProblemQueryHandler(
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<Testcase, Guid> testcaseRepo)
    {
        _cpRepo = cpRepo;
        _submissionRepo = submissionRepo;
        _resultRepo = resultRepo;
        _testcaseRepo = testcaseRepo;
    }

    public async Task<IoiProblemScoreDto?> Handle(
        ScoreIoiProblemQuery request,
        CancellationToken ct)
    {
        var cp = await _cpRepo.GetByIdAsync(request.ContestProblemId, ct);
        if (cp is null) return null;

        var submissions = await _submissionRepo.ListAsync(
            new SubmissionsByCpAndTeamSpec(request.ContestProblemId, request.TeamId), ct);

        if (submissions.Count == 0)
        {
            return new IoiProblemScoreDto(
                ContestProblemId: request.ContestProblemId,
                TeamId: request.TeamId,
                ScoringMode: "ioi",
                BestScore: 0,
                TotalSubmissions: 0,
                BestSubmissionId: null,
                BestSubmissionDetail: null,
                Submissions: new List<IoiSubmissionEntryDto>());
        }

        // Tính score cho mỗi submission, lấy max
        (int TotalScore, int PassedCases, int TotalCases, List<IoiCaseDto> Cases)? best = null;
        Guid? bestSubmissionId = null;
        var entries = new List<IoiSubmissionEntryDto>();

        foreach (var s in submissions)
        {
            var results = await _resultRepo.ListAsync(new ResultsBySubmissionSpec(s.Id), ct);

            var testcaseIds = results
                .Where(r => r.TestcaseId.HasValue)
                .Select(r => r.TestcaseId!.Value)
                .Distinct()
                .ToList();

            var testcaseInfo = testcaseIds.Count == 0
                ? new Dictionary<Guid, (int Ordinal, int Weight)>()
                : (await _testcaseRepo.ListAsync(new TestcasesByIdsSpec(testcaseIds), ct))
                    .ToDictionary(t => t.Id, t => (t.Ordinal, t.Weight));

            var score = ScoringHelper.CalcIoiScore(results, testcaseInfo);

            entries.Add(new IoiSubmissionEntryDto(
                SubmissionId: s.Id,
                SubmittedAt: s.CreatedAt,
                TotalScore: score.TotalScore,
                PassedCases: score.PassedCases,
                TotalCases: score.TotalCases));

            if (best is null || score.TotalScore > best.Value.TotalScore)
            {
                best = score;
                bestSubmissionId = s.Id;
            }
        }

        var bestDetail = best is null
            ? null
            : new IoiBestSubmissionDetailDto(
                PassedCases: best.Value.PassedCases,
                TotalCases: best.Value.TotalCases,
                Cases: best.Value.Cases);

        return new IoiProblemScoreDto(
            ContestProblemId: request.ContestProblemId,
            TeamId: request.TeamId,
            ScoringMode: "ioi",
            BestScore: best?.TotalScore ?? 0,
            TotalSubmissions: submissions.Count,
            BestSubmissionId: bestSubmissionId,
            BestSubmissionDetail: bestDetail,
            Submissions: entries);
    }
}
