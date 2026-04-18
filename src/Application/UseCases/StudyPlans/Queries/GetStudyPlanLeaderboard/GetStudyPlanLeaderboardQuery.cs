using MediatR;
using Application.UseCases.StudyPlans.Dtos;

namespace Application.UseCases.StudyPlans.Queries.GetStudyPlanLeaderboard;

public class GetStudyPlanLeaderboardQuery : IRequest<List<StudyPlanLeaderboardDto>>
{
    public Guid StudyPlanId { get; set; }
}