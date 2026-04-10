using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Reports.Dtos
{
    public class GetApprovedReportCountDto
    {
        public Guid TargetId { get; set; }
        public string TargetType { get; set; } = default!;
        public int ApprovedCount { get; set; }
    }
}
