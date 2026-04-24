using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.UseCases.ClassSlots.Specs;

namespace Application.UseCases.ClassSlots.Queries;

public class GetClassSemesterOverallRankingsQueryHandler
    : IRequestHandler<GetClassSemesterOverallRankingsQuery, GetClassSemesterOverallRankingsResponse>
{
    private readonly IReadRepository<ClassSemester, Guid> _classSemesterRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;

    public GetClassSemesterOverallRankingsQueryHandler(
        IReadRepository<ClassSemester, Guid> classSemesterRepo,
        IReadRepository<Submission, Guid> submissionRepo)
    {
        _classSemesterRepo = classSemesterRepo;
        _submissionRepo = submissionRepo;
    }

    public async Task<GetClassSemesterOverallRankingsResponse> Handle(
        GetClassSemesterOverallRankingsQuery request,
        CancellationToken ct)
    {
        // ======================
        // 1. GET CLASS SEMESTER
        // ======================
        var spec = new ClassSemesterByIdSpec(request.ClassSemesterId);
        var classSemester = await _classSemesterRepo.FirstOrDefaultAsync(spec, ct);

        if (classSemester == null)
            throw new KeyNotFoundException("CLASS_SEMESTER_NOT_FOUND");

        var classSlots = classSemester.ClassSlots
            .OrderBy(s => s.SlotNo)
            .ToList();

        // ======================
        // 2. GET ALL SUBMISSIONS
        // ======================
        var submissionSpec = new SubmissionsByClassSemesterSlotsSpec(
            classSlots.Select(slot => slot.Id).ToList());
        var allSubmissions = await _submissionRepo.ListAsync(submissionSpec, ct);

        // ======================
        // 3. BUILD OVERALL RANKINGS
        // ======================
        var rankings = new List<StudentOverallRankingDto>();

        foreach (var member in classSemester.ClassMembers.Where(cm => cm.IsActive))
        {
            var user = member.User;
            var memberSubs = allSubmissions.Where(s => s.UserId == user.UserId).ToList();

            var overallRanking = new StudentOverallRankingDto
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}".Trim(),
                AvatarUrl = user.AvatarUrl,
                TotalSolved = 0,
                TotalPenalty = 0,
                SlotStats = new()
            };

            // Calculate for each slot
            foreach (var slot in classSlots)
            {
                var slotSubs = memberSubs.Where(s => s.ClassSlotId == slot.Id).ToList();
                int slotSolved = 0;
                int slotPenalty = 0;

                foreach (var problem in slot.ClassSlotProblems)
                {
                    if (problem.Problem == null) continue;

                    var problemSubs = slotSubs
                        .Where(s => s.ProblemId == problem.ProblemId)
                        .OrderBy(s => s.CreatedAt)
                        .ToList();

                    int attempts = 0;
                    DateTime? acTime = null;

                    foreach (var sub in problemSubs)
                    {
                        attempts++;
                        if (sub.VerdictCode?.ToUpper() == "AC")
                        {
                            acTime = sub.CreatedAt;
                            break;
                        }
                    }

                    if (acTime != null)
                    {
                        slotSolved++;
                        var minutes = (int)(acTime.Value - (slot.OpenAt?.ToUniversalTime() ?? slot.CreatedAt)).TotalMinutes;
                        var problemPenalty = minutes + (attempts - 1) * 20;
                        slotPenalty += problemPenalty;
                    }
                }

                overallRanking.SlotStats.Add(new StudentSlotStatsDto
                {
                    SlotId = slot.Id,
                    SlotTitle = slot.Title,
                    Solved = slotSolved,
                    Penalty = slotPenalty
                });

                overallRanking.TotalSolved += slotSolved;
                overallRanking.TotalPenalty += slotPenalty;
            }

            rankings.Add(overallRanking);
        }

        // ======================
        // 4. SORT & RANK
        // ======================
        rankings = rankings
            .OrderByDescending(r => r.TotalSolved)
            .ThenBy(r => r.TotalPenalty)
            .ToList();

        int rank = 1;
        foreach (var r in rankings)
        {
            r.Rank = rank++;
        }

        // ======================
        // 5. BUILD RESPONSE
        // ======================
        var slotOverviews = classSlots
            .Select(s => new ClassSlotOverviewDto
            {
                SlotId = s.Id,
                SlotNo = s.SlotNo,
                Title = s.Title,
                DueAt = s.DueAt
            })
            .ToList();

        return new GetClassSemesterOverallRankingsResponse
        {
            ClassSemesterId = request.ClassSemesterId,
            ClassName = classSemester.Class.ClassCode,
            SubjectName = classSemester.Subject.Name,
            Slots = slotOverviews,
            Rankings = rankings,
            LastUpdated = DateTime.UtcNow
        };
    }
}
