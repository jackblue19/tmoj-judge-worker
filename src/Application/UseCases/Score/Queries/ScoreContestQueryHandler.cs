using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreContestQueryHandler
    : IRequestHandler<ScoreContestQuery, ContestScoreDto?>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<Testcase, Guid> _testcaseRepo;

    public ScoreContestQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<Testcase, Guid> testcaseRepo)
    {
        _contestRepo = contestRepo;
        _cpRepo = cpRepo;
        _submissionRepo = submissionRepo;
        _resultRepo = resultRepo;
        _testcaseRepo = testcaseRepo;
    }

    public async Task<ContestScoreDto?> Handle(
        ScoreContestQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);
        if (contest is null) return null;

        var scoringMode = ScoringHelper.IsAcmContest(contest) ? "acm" : "ioi";

        var contestProblems = await _cpRepo.ListAsync(
            new ContestProblemsByContestActiveSpec(request.ContestId), ct);

        if (scoringMode == "acm")
            return await BuildAcmContestAsync(request, contest, contestProblems, ct);

        return await BuildIoiContestAsync(request, contestProblems, ct);
    }

    private async Task<ContestScoreDto> BuildAcmContestAsync(
        ScoreContestQuery request,
        Contest contest,
        IReadOnlyList<ContestProblem> contestProblems,
        CancellationToken ct)
    {
        int totalScore = 0;
        int totalPenalty = 0;
        var entries = new List<ContestProblemScoreDto>();

        foreach (var cp in contestProblems)
        {
            var submissions = await _submissionRepo.ListAsync(
                new SubmissionsByCpAndTeamSpec(cp.Id, request.TeamId), ct);

            var r = ScoringHelper.CalcAcmProblem(submissions, contest.StartAt);

            if (r.Solved)
            {
                totalScore += 1;
                totalPenalty += r.PenaltyMinutes;
            }

            entries.Add(new ContestProblemScoreDto(
                ContestProblemId: cp.Id,
                Alias: cp.Alias,
                Ordinal: cp.Ordinal,
                BestScore: null,
                BestSubmissionId: null,
                PassedCases: null,
                TotalCases: null,
                Solved: r.Solved,
                Score: r.Score,
                WrongAttempts: r.WrongAttempts,
                PenaltyMinutes: r.PenaltyMinutes,
                FirstAcAt: r.FirstAcAt,
                TotalSubmissions: r.TotalSubmissions));
        }

        return new ContestScoreDto(
            ContestId: request.ContestId,
            TeamId: request.TeamId,
            ScoringMode: "acm",
            TotalScore: totalScore,
            TotalProblems: contestProblems.Count,
            TotalPenalty: totalPenalty,
            PenaltyFormula: ScoringHelper.AcmPenaltyFormula,
            SolvedCount: totalScore,
            Problems: entries);
    }

    private async Task<ContestScoreDto> BuildIoiContestAsync(
        ScoreContestQuery request,
        IReadOnlyList<ContestProblem> contestProblems,
        CancellationToken ct)
    {
        int totalScore = 0;
        var entries = new List<ContestProblemScoreDto>();

        foreach (var cp in contestProblems)
        {
            var submissions = await _submissionRepo.ListAsync(
                new SubmissionsByCpAndTeamSpec(cp.Id, request.TeamId), ct);

            (int TotalScore, int PassedCases, int TotalCases, List<IoiCaseDto> Cases)? best = null;
            Guid? bestSubId = null;

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

                if (best is null || score.TotalScore > best.Value.TotalScore)
                {
                    best = score;
                    bestSubId = s.Id;
                }
            }

            int problemBestScore = best?.TotalScore ?? 0;
            totalScore += problemBestScore;

            entries.Add(new ContestProblemScoreDto(
                ContestProblemId: cp.Id,
                Alias: cp.Alias,
                Ordinal: cp.Ordinal,
                BestScore: problemBestScore,
                BestSubmissionId: bestSubId,
                PassedCases: best?.PassedCases ?? 0,
                TotalCases: best?.TotalCases ?? 0,
                Solved: null,
                Score: null,
                WrongAttempts: null,
                PenaltyMinutes: null,
                FirstAcAt: null,
                TotalSubmissions: submissions.Count));
        }

        return new ContestScoreDto(
            ContestId: request.ContestId,
            TeamId: request.TeamId,
            ScoringMode: "ioi",
            TotalScore: totalScore,
            TotalProblems: contestProblems.Count,
            TotalPenalty: null,
            PenaltyFormula: null,
            SolvedCount: null,
            Problems: entries);
    }
}
