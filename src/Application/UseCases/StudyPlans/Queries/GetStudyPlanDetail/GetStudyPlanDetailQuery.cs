using MediatR;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanDetail;

public class GetStudyPlanDetailQuery : IRequest<StudyPlanDetailDto>
{
    public Guid StudyPlanId { get; set; }
    public Guid UserId { get; set; }
}