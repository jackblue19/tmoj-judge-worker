using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class PendingRecalcJobByContestSpec
    : Specification<ScoreRecalcJob>
{
    public PendingRecalcJobByContestSpec(Guid contestId)
    {
        Query
            .Where(j => j.ContestId == contestId && j.Status == "pending");
    }
}
