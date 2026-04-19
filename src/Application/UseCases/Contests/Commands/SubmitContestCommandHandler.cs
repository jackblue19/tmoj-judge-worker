using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Ardalis.Specification;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Commands;

public class SubmitContestCommandHandler
    : IRequestHandler<SubmitContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<ContestTeam, Guid> _ctRepo;
    private readonly IReadRepository<Testset, Guid> _testsetRepo;
    private readonly IWriteRepository<Submission, Guid> _submissionRepo;
    private readonly IWriteRepository<JudgeJob, Guid> _judgeJobRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SubmitContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<ContestTeam, Guid> ctRepo,
        IReadRepository<Testset, Guid> testsetRepo,
        IWriteRepository<Submission, Guid> submissionRepo,
        IWriteRepository<JudgeJob, Guid> judgeJobRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _cpRepo = cpRepo;
        _ctRepo = ctRepo;
        _testsetRepo = testsetRepo;
        _submissionRepo = submissionRepo;
        _judgeJobRepo = judgeJobRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(SubmitContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        var now = DateTime.UtcNow;

        // ======================
        // 1. CHECK CONTEST
        // ======================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);
        if (contest == null)
            throw new Exception("Contest not found");

        if (contest.VisibilityCode != "public")
            throw new Exception("Contest is not public");

        if (now < contest.StartAt)
            throw new Exception("Contest has not started");

        // Rule 5: submission hợp lệ dựa vào submitted_at <= end_at.
        if (now > contest.EndAt)
            throw new Exception("CONTEST_ENDED");

        // Rule 1/8: FREEZE KHÔNG chặn submit.
        // Freeze chỉ đóng băng scoreboard public — contestant vẫn submit/judge/xem verdict cá nhân.

        // ======================
        // 2. CHECK CONTEST PROBLEM
        // ======================
        var cp = await _cpRepo.GetByIdAsync(request.ContestProblemId, ct);
        if (cp == null || cp.ContestId != request.ContestId)
            throw new Exception("Contest problem not found");

        // Rule 2.3/10: problem access mode.
        if (cp.AccessMode == "read_only")
            throw new Exception("CONTEST_PROBLEM_READ_ONLY");

        if (cp.AccessMode == "hidden")
            throw new Exception("CONTEST_PROBLEM_HIDDEN");

        // ======================
        // 3. CHECK TEAM
        // ======================
        var team = await _ctRepo.FirstOrDefaultAsync(
            new ContestTeamByUserSpec(request.ContestId, userId.Value),
            ct);

        if (team == null)
            throw new Exception("You have not joined this contest");

        // ======================
        // 4. GET TESTSET
        // ======================
        var testset = await _testsetRepo.FirstOrDefaultAsync(
            new TestsetByProblemSpec(cp.ProblemId),
            ct);

        if (testset == null)
            throw new Exception("No active testset found");

        // ======================
        // 5. CREATE SUBMISSION + JOB
        // ======================
        var submissionId = Guid.NewGuid();
        var judgeJobId = Guid.NewGuid();

        var submission = new Submission
        {
            Id = submissionId,
            UserId = userId.Value,
            ProblemId = cp.ProblemId,
            ContestProblemId = cp.Id,
            TeamId = team.TeamId,
            SourceCode = request.Code,

            StatusCode = "queued",
            VerdictCode = null,
            SubmissionType = "contest",

            CodeSize = request.Code?.Length ?? 0,
            CodeHash = Guid.NewGuid().ToString(),
            CreatedAt = now,

            TestsetId = cp.OverrideTestsetId ?? testset.Id
        };

        var judgeJob = new JudgeJob
        {
            Id = judgeJobId,
            SubmissionId = submissionId,
            EnqueueAt = now,
            Status = "queued",
            Attempts = 0,
            Priority = 0,
            TriggeredByUserId = userId.Value,
            TriggerType = "submit"
        };

        await _submissionRepo.AddAsync(submission, ct);
        await _judgeJobRepo.AddAsync(judgeJob, ct);

        await _uow.SaveChangesAsync(ct);

        return submissionId;
    }
}