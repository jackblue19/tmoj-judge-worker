using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class ReportByUserAndTargetSpec : Specification<ContentReport>
{
    public ReportByUserAndTargetSpec(Guid userId, Guid targetId, string targetType)
    {
        Query.Where(x =>
            x.ReporterId == userId &&
            x.TargetId == targetId &&
            x.TargetType == targetType);
    }
}