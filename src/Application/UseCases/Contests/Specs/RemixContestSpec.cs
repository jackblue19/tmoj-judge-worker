using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class RemixContestSpec
    : Specification<Contest>, ISingleResultSpecification<Contest>
{
    public RemixContestSpec(Guid contestId)
    {
        Query
            .Where(c => c.Id == contestId)

            // chỉ lấy problem để clone (KHÔNG include Problem để nhẹ query)
            .Include(c => c.ContestProblems!
                .Where(cp => cp.IsActive))

            .AsNoTracking();
    }
}