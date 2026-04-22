using MediatR;

namespace Application.UseCases.StudyPlans.Commands.EnrollStudyPlan;

public class EnrollStudyPlanCommand : IRequest<Unit>
{
    public Guid StudyPlanId { get; set; }
}