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
    private readonly IReadRepository<ClassSlot, Guid> _classSlotRepo;
    private readonly IReadRepository<ClassMember, Guid> _classMemberRepo;
    private readonly IReadRepository<Runtime, Guid> _runtimeRepo;
    private readonly IWriteRepository<Submission, Guid> _submissionRepo;
    private readonly IWriteRepository<JudgeJob, Guid> _judgeJobRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SubmitContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<ContestTeam, Guid> ctRepo,
        IReadRepository<Testset, Guid> testsetRepo,
        IReadRepository<ClassSlot, Guid> classSlotRepo,
        IReadRepository<ClassMember, Guid> classMemberRepo,
        IReadRepository<Runtime, Guid> runtimeRepo,
        IWriteRepository<Submission, Guid> submissionRepo,
        IWriteRepository<JudgeJob, Guid> judgeJobRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _cpRepo = cpRepo;
        _ctRepo = ctRepo;
        _testsetRepo = testsetRepo;
        _classSlotRepo = classSlotRepo;
        _classMemberRepo = classMemberRepo;
        _runtimeRepo = runtimeRepo;
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

        // Private contest chỉ submit được khi đi qua class slot (slot.ContestId == contest.Id).
        if (contest.VisibilityCode != "public" && request.ClassSlotId is null)
            throw new Exception("Contest is not public");

        if (now < contest.StartAt)
            throw new Exception("Contest has not started");

        // Rule 5: submission hợp lệ dựa vào submitted_at <= end_at.
        if (now > contest.EndAt)
            throw new Exception("CONTEST_ENDED");

        // Class contest: freeze chặn submit của student.
        if (request.ClassSlotId is not null && contest.FreezeAt.HasValue && now >= contest.FreezeAt.Value)
            throw new Exception("CONTEST_FROZEN");

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
        // 2b. CHECK CLASS SLOT (nếu submit qua class slot)
        // ======================
        if (request.ClassSlotId is Guid classSlotId)
        {
            var slot = await _classSlotRepo.GetByIdAsync(classSlotId, ct);
            if (slot is null)
                throw new Exception("Class slot not found");

            if (slot.ContestId != request.ContestId)
                throw new Exception("Class slot does not host this contest");

            var isMember = await _classMemberRepo.AnyAsync(
                new ClassMemberByUserSpec(slot.ClassSemesterId, userId.Value),
                ct);

            if (!isMember)
                throw new Exception("You are not a member of this class");
        }

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
        // 5. GET RUNTIME BY LANGUAGE
        // ======================
        var runtime = await _runtimeRepo.FirstOrDefaultAsync(
            new RuntimeByNameSpec(request.Language), ct);

        if (runtime == null)
            throw new Exception("LANGUAGE_NOT_SUPPORTED");

        // ======================
        // 6. CREATE SUBMISSION + JOB
        // ======================
        var submissionId = Guid.NewGuid();
        var judgeJobId = Guid.NewGuid();

        var submission = new Submission
        {
            Id = submissionId,
            UserId = userId.Value,
            ProblemId = cp.ProblemId,
            ContestProblemId = cp.Id,
            ClassSlotId = request.ClassSlotId,
            TeamId = team.TeamId,
            SourceCode = request.Code,
            RuntimeId = runtime.Id,

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