using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class ScoreAcmProblemQueryHandler
    : IRequestHandler<ScoreAcmProblemQuery, AcmProblemScoreDto?>
{
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;

    public ScoreAcmProblemQueryHandler(
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Submission, Guid> submissionRepo)
    {
        _cpRepo = cpRepo;
        _submissionRepo = submissionRepo;
    }

    public async Task<AcmProblemScoreDto?> Handle(
        ScoreAcmProblemQuery request,
        CancellationToken ct)
    {
        var cp = await _cpRepo.FirstOrDefaultAsync(
            new ContestProblemByIdWithContestSpec(request.ContestProblemId), ct);
        if (cp is null) return null;

        var submissions = await _submissionRepo.ListAsync(
            new SubmissionsByCpAndTeamSpec(request.ContestProblemId, request.TeamId), ct);

        var result = ScoringHelper.CalcAcmProblem(submissions, cp.Contest.StartAt);

        return new AcmProblemScoreDto(
            ContestProblemId: request.ContestProblemId,
            TeamId: request.TeamId,
            ScoringMode: "acm",
            PenaltyFormula: ScoringHelper.AcmPenaltyFormula,
            Solved: result.Solved,
            Score: result.Score,
            WrongAttempts: result.WrongAttempts,
            PenaltyMinutes: result.PenaltyMinutes,
            FirstAcAt: result.FirstAcAt,
            TotalSubmissions: result.TotalSubmissions,
            SubmissionHistory: result.SubmissionHistory);
    }
}
