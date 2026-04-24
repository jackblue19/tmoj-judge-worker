using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class ReportsByTargetAndStatusSpec : Specification<ContentReport>
{
    public ReportsByTargetAndStatusSpec(Guid targetId, string targetType, string status)
    {
        Query.Where(x =>
            x.TargetId == targetId &&
            x.TargetType != null &&
            x.Status != null &&
            x.TargetType == targetType &&
            x.Status == status);
    }
}