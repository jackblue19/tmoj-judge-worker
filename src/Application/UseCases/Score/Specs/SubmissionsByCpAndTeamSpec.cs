using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Score.Specs;

/// <summary>
/// Lấy tất cả submission status="done" của team cho 1 ContestProblem, sắp xếp theo CreatedAt.
/// </summary>
public sealed class SubmissionsByCpAndTeamSpec : Specification<Submission>
{
    public SubmissionsByCpAndTeamSpec(Guid contestProblemId, Guid teamId)
    {
        Query.Where(s => s.ContestProblemId == contestProblemId
                         && s.TeamId == teamId
                         && s.StatusCode == "done")
             .OrderBy(s => s.CreatedAt);
    }
}
