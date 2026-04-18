using MediatR;

namespace Application.UseCases.StudyProgress.Commands.CompleteStudyPlanItem;

public class CompleteStudyPlanItemCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public Guid StudyPlanItemId { get; set; }
}