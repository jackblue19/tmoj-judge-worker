using MediatR;

namespace Application.UseCases.StudyPlans.Commands.CreateStudyPlan;

public class CreateStudyPlanCommand : IRequest<Guid>
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsPaid { get; set; }
    public int Price { get; set; }
}