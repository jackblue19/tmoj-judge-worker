using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class AllReportsSpec : Specification<ContentReport>
{
    public AllReportsSpec(string? status)
    {
        if (!string.IsNullOrEmpty(status))
        {
            Query.Where(x => x.Status.ToLower() == status.ToLower());
        }

        Query.OrderByDescending(x => x.CreatedAt);
    }
}