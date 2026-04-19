using MediatR;

namespace Application.UseCases.StudyProgress.Commands.ResetStudyProgress;

public class ResetStudyProgressCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public Guid StudyPlanId { get; set; }
}