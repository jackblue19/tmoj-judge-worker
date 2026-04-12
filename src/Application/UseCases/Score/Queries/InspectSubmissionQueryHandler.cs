using Application.UseCases.Score.Dtos;
using Application.UseCases.Score.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Score.Queries;

public sealed class InspectSubmissionQueryHandler
    : IRequestHandler<InspectSubmissionQuery, InspectSubmissionDto?>
{
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<JudgeRun, Guid> _judgeRunRepo;
    private readonly IReadRepository<JudgeJob, Guid> _judgeJobRepo;

    public InspectSubmissionQueryHandler(
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<JudgeRun, Guid> judgeRunRepo,
        IReadRepository<JudgeJob, Guid> judgeJobRepo)
    {
        _submissionRepo = submissionRepo;
        _resultRepo = resultRepo;
        _judgeRunRepo = judgeRunRepo;
        _judgeJobRepo = judgeJobRepo;
    }

    public async Task<InspectSubmissionDto?> Handle(
        InspectSubmissionQuery request,
        CancellationToken ct)
    {
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return null;

        var rawResults = await _resultRepo.ListAsync(
            new AllResultsBySubmissionSpec(request.SubmissionId), ct);

        var judgeRuns = await _judgeRunRepo.ListAsync(
            new JudgeRunsBySubmissionSpec(request.SubmissionId), ct);

        var judgeJobs = await _judgeJobRepo.ListAsync(
            new JudgeJobsBySubmissionSpec(request.SubmissionId), ct);

        return new InspectSubmissionDto(
            Submission: new InspectSubmissionInfoDto(
                Id: submission.Id,
                StatusCode: submission.StatusCode,
                VerdictCode: submission.VerdictCode,
                FinalScore: submission.FinalScore,
                JudgedAt: submission.JudgedAt,
                TestsetId: submission.TestsetId,
                ProblemId: submission.ProblemId,
                ContestProblemId: submission.ContestProblemId),
            ResultCount: rawResults.Count,
            Results: rawResults.Select(r => new InspectResultDto(
                Id: r.Id,
                TestcaseId: r.TestcaseId,
                StatusCode: r.StatusCode,
                Type: r.Type,
                RuntimeMs: r.RuntimeMs,
                MemoryKb: r.MemoryKb,
                Message: r.Message,
                JudgeRunId: r.JudgeRunId)).ToList(),
            JudgeRunCount: judgeRuns.Count,
            JudgeRuns: judgeRuns.Select(jr => new InspectJudgeRunDto(
                Id: jr.Id,
                Status: jr.Status,
                StartedAt: jr.StartedAt,
                FinishedAt: jr.FinishedAt,
                CompileExitCode: jr.CompileExitCode,
                CompileTimeMs: jr.CompileTimeMs,
                TotalTimeMs: jr.TotalTimeMs,
                TotalMemoryKb: jr.TotalMemoryKb,
                Note: jr.Note)).ToList(),
            JudgeJobs: judgeJobs.Select(jj => new InspectJudgeJobDto(
                Id: jj.Id,
                Status: jj.Status,
                LastError: jj.LastError,
                EnqueueAt: jj.EnqueueAt)).ToList());
    }
}
