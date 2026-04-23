using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetDailyActivities;

public class GetDailyActivitiesQuery : IRequest<List<DailyActivityDto>>
{
    public int? Year { get; set; }
}
