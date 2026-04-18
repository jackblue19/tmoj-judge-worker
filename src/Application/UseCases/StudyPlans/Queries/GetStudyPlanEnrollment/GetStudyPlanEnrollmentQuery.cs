using MediatR;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanEnrollment;

public class GetStudyPlanEnrollmentQuery : IRequest<StudyPlanEnrollmentDto>
{
    public Guid UserId { get; set; }
    public Guid StudyPlanId { get; set; }
}