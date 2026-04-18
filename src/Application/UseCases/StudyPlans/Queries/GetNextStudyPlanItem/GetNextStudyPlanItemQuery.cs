using Application.UseCases.StudyPlans.Dtos;
using MediatR;

namespace Application.UseCases.StudyPlans.Queries.GetNextStudyPlanItem;

public class GetNextStudyPlanItemQuery : IRequest<NextStudyPlanItemDto>
{
    public Guid StudyPlanId { get; set; }
    public Guid StudyPlanItemId { get; set; }
}