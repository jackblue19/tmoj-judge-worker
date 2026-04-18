using MediatR;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlans;

public class GetStudyPlansQuery : IRequest<List<StudyPlanDto>>
{
    public Guid? CreatorId { get; set; }
}