using MediatR;
using System;

namespace Application.UseCases.StudyPlans.Commands.RemoveProblemFromPlan;

public class RemoveProblemFromPlanCommand : IRequest<bool>
{
    public Guid StudyPlanId { get; set; }
    public Guid ProblemId { get; set; }
}
