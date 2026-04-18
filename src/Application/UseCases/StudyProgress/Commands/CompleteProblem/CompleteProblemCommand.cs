using MediatR;

namespace Application.UseCases.StudyProgress.Commands.CompleteProblem;

public class CompleteProblemCommand : IRequest<Unit>
{
    public Guid StudyPlanItemId { get; set; }
    public Guid UserId { get; set; }
}