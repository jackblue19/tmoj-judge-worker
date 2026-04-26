using MediatR;
using System;

namespace Application.UseCases.StudyPlans.Commands.DeleteStudyPlan;

public class DeleteStudyPlanCommand : IRequest<bool>
{
    public Guid StudyPlanId { get; set; }
}
