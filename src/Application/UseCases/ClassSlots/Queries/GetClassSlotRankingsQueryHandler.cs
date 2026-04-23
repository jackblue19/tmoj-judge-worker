using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.UseCases.ClassSlots.Specs;

namespace Application.UseCases.ClassSlots.Queries;

public class GetClassSlotRankingsQueryHandler
    : IRequestHandler<GetClassSlotRankingsQuery, GetClassSlotRankingsResponse>
{
    private readonly IReadRepository<ClassSlot, Guid> _classSlotRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;

    public GetClassSlotRankingsQueryHandler(
        IReadRepository<ClassSlot, Guid> classSlotRepo,
        IReadRepository<Submission, Guid> submissionRepo)
    {
        _classSlotRepo = classSlotRepo;
        _submissionRepo = submissionRepo;
    }

    public async Task<GetClassSlotRankingsResponse> Handle(
        GetClassSlotRankingsQuery request,
        CancellationToken ct)
    {
        // ======================
        // 1. GET CLASS SLOT
        // ======================
        var spec = new ClassSlotByIdSpec(request.ClassSlotId);
        var classSlot = await _classSlotRepo.FirstOrDefaultAsync(spec, ct);

        if (classSlot == null)
            throw new KeyNotFoundException("CLASS_SLOT_NOT_FOUND");

        // ======================
        // 2. GET SUBMISSIONS
        // ======================
        var submissionSpec = new SubmissionsByClassSlotIdSpec(request.ClassSlotId);
        var submissions = await _submissionRepo.ListAsync(submissionSpec, ct);

        // ======================
        // 3. GET CLASS MEMBERS
        // ======================
        var classMembers = classSlot.ClassSemester.ClassMembers
            .Where(cm => cm.IsActive)
            .ToList();

        // ======================
        // 4. BUILD RANKINGS
        // ======================
        var rankings = new List<StudentRankingDto>();

        foreach (var member in classMembers)
        {
            var user = member.User;
            var memberSubs = submissions.Where(s => s.UserId == user.UserId).ToList();

            var studentRanking = new StudentRankingDto
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}".Trim(),
                AvatarUrl = user.AvatarUrl,
                Solved = 0,
                Penalty = 0,
                Problems = new()
            };

            int solved = 0;
            int totalPenalty = 0;

            foreach (var csp in classSlot.ClassSlotProblems)
            {
                if (csp.Problem == null) continue;

                var problemSubs = memberSubs
                    .Where(s => s.ProblemId == csp.ProblemId)
                    .OrderBy(s => s.CreatedAt)
                    .ToList();

                int attempts = 0;
                DateTime? acTime = null;
                int problemPenalty = 0;

                foreach (var sub in problemSubs)
                {
                    attempts++;
                    var verdict = sub.VerdictCode?.ToUpper();

                    if (verdict == "AC")
                    {
                        acTime = sub.CreatedAt;
                        break;
                    }
                }

                bool isSolved = acTime != null;

                if (isSolved)
                {
                    solved++;
                    var baseTime = classSlot.OpenAt?.ToUniversalTime() ?? classSlot.CreatedAt;
                    var minutes = (int)(acTime.Value - baseTime).TotalMinutes;
                    problemPenalty = minutes + (attempts - 1) * 20;
                    totalPenalty += problemPenalty;
                }

                studentRanking.Problems.Add(new ProblemRankingDto
                {
                    ProblemId = csp.Problem.Id,
                    Alias = "",
                    Title = csp.Problem.Title ?? "",
                    IsSolved = isSolved,
                    Attempts = attempts,
                    PenaltyTime = isSolved ? problemPenalty : null
                });
            }

            studentRanking.Solved = solved;
            studentRanking.Penalty = totalPenalty;
            rankings.Add(studentRanking);
        }

        // ======================
        // 5. SORT & RANK
        // ======================
        rankings = rankings
            .OrderByDescending(r => r.Solved)
            .ThenBy(r => r.Penalty)
            .ToList();

        int rank = 1;
        foreach (var r in rankings)
        {
            r.Rank = rank++;
        }

        return new GetClassSlotRankingsResponse
        {
            ClassSlotId = request.ClassSlotId,
            SlotTitle = classSlot.Title,
            SlotDescription = classSlot.Description,
            DueAt = classSlot.DueAt,
            Rankings = rankings,
            LastUpdated = DateTime.UtcNow
        };
    }
}
