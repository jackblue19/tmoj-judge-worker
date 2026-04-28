using Application.Common.Interfaces;
using Application.UseCases.Classes.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Classes.Commands.CreateClassContest;

public class CreateClassContestCommandHandler
    : IRequestHandler<CreateClassContestCommand, (Guid ContestId, Guid SlotId)>
{
    private readonly IReadRepository<ClassSemester, Guid> _classSemesterRepo;
    private readonly IReadRepository<Problem, Guid> _problemRepo;
    private readonly IReadRepository<ClassSlot, Guid> _classSlotRepo;
    private readonly IReadRepository<ClassMember, Guid> _classMemberRepo;
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly IWriteRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _contestProblemRepo;
    private readonly IWriteRepository<ClassSlot, Guid> _classSlotWriteRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly IWriteRepository<TeamMember, Guid> _teamMemberRepo;
    private readonly IWriteRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IUnitOfWork _uow;

    public CreateClassContestCommandHandler(
        IReadRepository<ClassSemester, Guid> classSemesterRepo,
        IReadRepository<Problem, Guid> problemRepo,
        IReadRepository<ClassSlot, Guid> classSlotRepo,
        IReadRepository<ClassMember, Guid> classMemberRepo,
        IReadRepository<Team, Guid> teamRepo,
        IWriteRepository<Contest, Guid> contestRepo,
        IWriteRepository<ContestProblem, Guid> contestProblemRepo,
        IWriteRepository<ClassSlot, Guid> classSlotWriteRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        IWriteRepository<TeamMember, Guid> teamMemberRepo,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IUnitOfWork uow)
    {
        _classSemesterRepo = classSemesterRepo;
        _problemRepo = problemRepo;
        _classSlotRepo = classSlotRepo;
        _classMemberRepo = classMemberRepo;
        _teamRepo = teamRepo;
        _contestRepo = contestRepo;
        _contestProblemRepo = contestProblemRepo;
        _classSlotWriteRepo = classSlotWriteRepo;
        _teamWriteRepo = teamWriteRepo;
        _teamMemberRepo = teamMemberRepo;
        _contestTeamRepo = contestTeamRepo;
        _uow = uow;
    }

    public async Task<(Guid ContestId, Guid SlotId)> Handle(
        CreateClassContestCommand request, CancellationToken ct)
    {
        // ── 1. Validate ──────────────────────────────────────────
        var classSemester = await _classSemesterRepo.GetByIdAsync(request.ClassSemesterId, ct)
            ?? throw new KeyNotFoundException("Class instance not found.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (request.StartAt >= request.EndAt)
            throw new ArgumentException("EndAt must be after StartAt.");

        // ── 2. Build Contest ─────────────────────────────────────
        var contest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim(),
            DescriptionMd = request.DescriptionMd?.Trim(),
            VisibilityCode = "private",
            ContestType = "acm",
            AllowTeams = false,
            StartAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc),
            EndAt = DateTime.SpecifyKind(request.EndAt, DateTimeKind.Utc),
            FreezeAt = request.FreezeAt.HasValue
                ? DateTime.SpecifyKind(request.FreezeAt.Value, DateTimeKind.Utc)
                : null,
            Rules = request.Rules?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedByUserId
        };
        await _contestRepo.AddAsync(contest, ct);

        // ── 3. Build ContestProblems ─────────────────────────────
        if (request.Problems is { Count: > 0 })
        {
            int ord = 1;
            foreach (var p in request.Problems)
            {
                var problem = await _problemRepo.GetByIdAsync(p.ProblemId, ct)
                    ?? throw new KeyNotFoundException($"Problem {p.ProblemId} not found.");

                await _contestProblemRepo.AddAsync(new ContestProblem
                {
                    ContestId = contest.Id,
                    ProblemId = problem.Id,
                    Ordinal = p.Ordinal ?? ord,
                    Alias = p.Alias ?? ((char)('A' + ord - 1)).ToString(),
                    Points = p.Points ?? 100,
                    MaxScore = p.MaxScore ?? 100,
                    TimeLimitMs = p.TimeLimitMs ?? problem.TimeLimitMs,
                    MemoryLimitKb = p.MemoryLimitKb ?? problem.MemoryLimitKb,
                    IsActive = true,
                    CreatedBy = request.CreatedByUserId
                }, ct);
                ord++;
            }
        }

        // ── 4. Build ClassSlot ───────────────────────────────────
        var existingSlotCount = await _classSlotRepo.CountAsync(
            new ClassSlotsForSemesterSpec(request.ClassSemesterId), ct);

        var slotNo = request.SlotNo ?? (existingSlotCount + 1);

        var slot = new ClassSlot
        {
            ClassSemesterId = request.ClassSemesterId,
            SlotNo = slotNo,
            Title = request.SlotTitle ?? request.Title.Trim(),
            Mode = "contest",
            ContestId = contest.Id,
            IsPublished = false,
            CreatedBy = request.CreatedByUserId,
            UpdatedBy = request.CreatedByUserId
        };
        await _classSlotWriteRepo.AddAsync(slot, ct);

        // ── 5. Auto-enroll tất cả active members ─────────────────
        // Batch 1: lấy tất cả active members kèm User (1 query)
        var members = await _classMemberRepo.ListAsync(
            new ActiveClassMembersWithUserSpec(request.ClassSemesterId), ct);

        if (members.Count > 0)
        {
            var memberIds = members.Select(m => m.UserId).ToList();

            // Batch 2: lấy personal team đã tồn tại (1 query)
            var existingPersonalTeams = await _teamRepo.ListAsync(
                new PersonalTeamsByLeadersSpec(memberIds), ct);

            var personalTeamByUserId = existingPersonalTeams
                .ToDictionary(t => t.LeaderId);

            foreach (var member in members)
            {
                if (!personalTeamByUserId.TryGetValue(member.UserId, out var personalTeam))
                {
                    // Tạo personal team mới cho member chưa có
                    personalTeam = new Team
                    {
                        LeaderId = member.UserId,
                        TeamSize = 1,
                        TeamName = member.User?.DisplayName ?? "Personal Team",
                        IsPersonal = true
                    };
                    await _teamWriteRepo.AddAsync(personalTeam, ct);
                    await _teamMemberRepo.AddAsync(
                        new TeamMember { TeamId = personalTeam.Id, UserId = member.UserId }, ct);

                    personalTeamByUserId[member.UserId] = personalTeam;
                }

                // Contest mới tạo → không cần kiểm tra đã join chưa
                await _contestTeamRepo.AddAsync(
                    new ContestTeam { ContestId = contest.Id, TeamId = personalTeam.Id }, ct);
            }
        }

        // ── 6. Commit một transaction duy nhất ───────────────────
        await _uow.SaveChangesAsync(ct);

        return (contest.Id, slot.Id);
    }
}
