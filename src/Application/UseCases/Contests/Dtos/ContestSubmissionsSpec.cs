using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestSubmissionsSpec : Specification<Submission>
{
    public ContestSubmissionsSpec(Guid contestId)
    {
        Query
            .Where(x =>
                x.SubmissionType == "contest" &&
                x.TeamId != null &&
                x.ContestProblemId != null &&
                x.ContestProblem!.ContestId == contestId &&
                x.VerdictCode != null
            );
    }
}