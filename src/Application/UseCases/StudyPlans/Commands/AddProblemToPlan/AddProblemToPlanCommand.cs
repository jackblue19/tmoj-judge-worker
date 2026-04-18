using MediatR;

namespace Application.UseCases.StudyPlans.Commands.AddProblemToPlan;

public class AddProblemToPlanCommand : IRequest<Unit>
{
    public Guid StudyPlanId { get; set; }
    public Guid ProblemId { get; set; }
}