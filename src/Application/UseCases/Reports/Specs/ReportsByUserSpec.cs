using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class ReportsByUserSpec : Specification<ContentReport>
{
    public ReportsByUserSpec(Guid userId)
    {
        Query.Where(x => x.ReporterId == userId)
             .OrderByDescending(x => x.CreatedAt);
    }
}