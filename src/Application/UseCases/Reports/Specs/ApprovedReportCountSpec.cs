using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Reports.Specs
{
    public class ApprovedReportCountSpec : Specification<ContentReport>
    {
        public ApprovedReportCountSpec(Guid targetId, string targetType)
        {
            Query.Where(x =>
                x.TargetId == targetId &&
                x.TargetType == targetType &&
                x.Status == "approved");
        }
    }
}
