using MediatR;

namespace Application.UseCases.StudyPlans.Commands.BuyStudyPlan;

public class BuyStudyPlanCommand : IRequest<Unit>
{
    public Guid StudyPlanId { get; set; }
}