using MediatR;

namespace Application.UseCases.StudyProgress.Queries.GetNextStudyItem;

public class GetNextStudyItemQuery : IRequest<Guid?>
{
    public Guid UserId { get; set; }
    public Guid StudyPlanItemId { get; set; }
}