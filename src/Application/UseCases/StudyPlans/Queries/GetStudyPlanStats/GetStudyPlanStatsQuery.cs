using MediatR;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanStats;

public class GetStudyPlanStatsQuery : IRequest<StudyPlanStatsDto>
{
    public Guid StudyPlanId { get; set; }
}