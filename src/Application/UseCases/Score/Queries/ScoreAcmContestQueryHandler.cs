using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreAcmContestQueryHandler
    : IRequestHandler<ScoreAcmContestQuery, AcmContestScoreDto?>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;

    public ScoreAcmContestQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Submission, Guid> submissionRepo)
    {
        _contestRepo = contestRepo;
        _cpRepo = cpRepo;
        _submissionRepo = submissionRepo;
    }

    public async Task<AcmContestScoreDto?> Handle(
        ScoreAcmContestQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);
        if (contest is null) return null;

        var contestProblems = await _cpRepo.ListAsync(
            new ContestProblemsByContestActiveSpec(request.ContestId), ct);

        int totalScore = 0;
        int totalPenalty = 0;
        var entries = new List<AcmContestProblemEntryDto>();

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

            entries.Add(new AcmContestProblemEntryDto(
                ContestProblemId: cp.Id,
                Alias: cp.Alias,
                Ordinal: cp.Ordinal,
                Solved: r.Solved,
                Score: r.Score,
                WrongAttempts: r.WrongAttempts,
                PenaltyMinutes: r.PenaltyMinutes,
                FirstAcAt: r.FirstAcAt,
                TotalSubmissions: r.TotalSubmissions));
        }

        return new AcmContestScoreDto(
            ContestId: request.ContestId,
            TeamId: request.TeamId,
            ScoringMode: "acm",
            TotalScore: totalScore,
            TotalPenalty: totalPenalty,
            PenaltyFormula: ScoringHelper.AcmPenaltyFormula,
            SolvedCount: totalScore,
            TotalProblems: contestProblems.Count,
            Problems: entries);
    }
}
