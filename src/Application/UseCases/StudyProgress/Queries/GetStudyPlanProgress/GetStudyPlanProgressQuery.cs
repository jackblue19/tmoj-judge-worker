using Application.UseCases.StudyProgress.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.StudyProgress.Queries.GetStudyPlanProgress
{
    public class GetStudyPlanProgressQuery : IRequest<StudyPlanProgressDto>
    {
        public Guid UserId { get; set; }
        public Guid StudyPlanId { get; set; }
    }
}
