using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Reports.Dtos
{
    public class ModerationActionByReportSpec : Specification<ModerationAction>
    {
        public ModerationActionByReportSpec(Guid reportId, string actionType)
        {
            Query.Where(x =>
                x.ReportId == reportId &&
                x.ActionType == actionType);
        }
    }
}
