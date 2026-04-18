using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.StudyPlans.Commands.AddStudyPlan
{
    public class AddStudyPlanCommand : IRequest<Guid>
    {
        public Guid CreatorId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPaid { get; set; }
        public int Price { get; set; }
    }
}
