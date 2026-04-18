using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.StudyPlans.Dtos
{
    public class NextStudyPlanItemDto
    {
        public bool HasNext { get; set; }
        public string Message { get; set; } = string.Empty;

        public Guid? NextItemId { get; set; }
        public Guid? ProblemId { get; set; }
        public int? Order { get; set; }
    }
}
