using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Policies;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestDetailQueryHandler
    : IRequestHandler<GetContestDetailQuery, ContestDetailDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IContestRepository _contestQueryRepo;
    private readonly IContestStatusService _statusService;
    private readonly ICurrentUserService _currentUser;

    public GetContestDetailQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IContestRepository contestQueryRepo,
        IContestStatusService statusService,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _contestQueryRepo = contestQueryRepo;
        _statusService = statusService;
        _currentUser = currentUser;
    }

    public async Task<ContestDetailDto> Handle(
        GetContestDetailQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.FirstOrDefaultAsync(
            new GetContestDetailSpec(request.ContestId),
            ct);

        if (contest == null)
            throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var isPrivileged =
            _currentUser.IsAuthenticated &&
            (_currentUser.IsInRole("admin") || _currentUser.IsInRole("manager"));

        var isCreator =
            _currentUser.IsAuthenticated &&
            contest.CreatedBy == _currentUser.UserId;

        if (!contest.IsActive && !isPrivileged)
            throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        // Visibility guard cho non-class contests.
        // "hidden"  → chỉ admin/manager/creator
        // "private" → admin/manager/creator hoặc đã đăng ký tham gia
        // "public"  → ai cũng xem được
        if (!isPrivileged && !isCreator)
        {
            var visibility = contest.VisibilityCode ?? "private";
            if (visibility == "hidden")
                throw new KeyNotFoundException("CONTEST_NOT_FOUND");

            if (visibility == "private")
            {
                var userId = _currentUser.UserId;
                var isParticipant = userId.HasValue &&
                    await _contestQueryRepo.HasUserRegisteredAsync(contest.Id, userId.Value);

                if (!isParticipant)
                    throw new KeyNotFoundException("CONTEST_NOT_FOUND");
            }
        }

        var isFrozen = FreezeContestPatch.IsFrozen(contest);

        var status = _statusService.GetStatus(contest.StartAt, contest.EndAt);
        var phase = _statusService.GetPhase(contest.StartAt, contest.EndAt);
        var canJoin = _statusService.CanJoin(contest.StartAt, contest.EndAt);

        var problems = (contest.ContestProblems ?? new List<ContestProblem>())
            .Where(cp => cp.IsActive)
            .OrderBy(cp => cp.DisplayIndex ?? cp.Ordinal ?? 999)
            .ThenBy(cp => cp.Alias)
            .ToList();

        var (totalTeams, totalMembers) =
            await _contestQueryRepo.GetContestParticipantCountsAsync(contest.Id);

        // ❌ REMOVE:
        // FreezeContestPatch.EnsureViewAllowed(contest);

        return new ContestDetailDto
        {
            Id = contest.Id,
            Title = contest.Title ?? "",
            Description = contest.DescriptionMd ?? "",
            Slug = !string.IsNullOrWhiteSpace(contest.Slug)
                ? contest.Slug
                : $"{SlugHelper.Generate(contest.Title ?? "contest")}-{contest.Id.ToString()[..6]}",

            Visibility = contest.VisibilityCode ?? "private",
            ContestType = contest.ContestType ?? "icpc",
            AllowTeams = contest.AllowTeams,

            InviteCode = (isPrivileged || isCreator) ? contest.InviteCode : null,

            Status = status,
            Phase = phase,

            IsPublished = contest.VisibilityCode == "public",

            // ✅ chỉ expose flag cho FE
            IsFrozen = isFrozen,

            // ⚠️ freeze KHÔNG ảnh hưởng join rule (tùy business)
            CanJoin = canJoin,
            IsRegistered = false,
            HasLeaderboard = true,

            StartAt = contest.StartAt,
            EndAt = contest.EndAt,
            DurationMinutes = (int)(contest.EndAt - contest.StartAt).TotalMinutes,

            ProblemCount = problems.Count,
            TotalPoints = problems.Sum(p => p.Points ?? 0),

            TotalTeams = totalTeams,
            TotalMembers = totalMembers,

            // ✅ ALWAYS return problems
            Problems = problems.Select(cp => new ContestProblemDto
            {
                Id = cp.Id,
                ProblemId = cp.ProblemId,
                Title = cp.Problem != null ? cp.Problem.Title : "Unknown",
                Alias = cp.Alias ?? "",
                Ordinal = cp.Ordinal,
                DisplayIndex = cp.DisplayIndex,
                Points = cp.Points ?? 0,
                TimeLimitMs = cp.TimeLimitMs,
                MemoryLimitKb = cp.MemoryLimitKb,
                Status = "not_started"
            }).ToList()
        };
    }
}