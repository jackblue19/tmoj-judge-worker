using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Reports.Specs;

public class ModerationActionByReportAndTypeSpec
    : Specification<ModerationAction>
{
    public ModerationActionByReportAndTypeSpec(Guid reportId, string actionType)
    {
        Query.Where(x => x.ReportId == reportId && x.ActionType == actionType);
    }
}