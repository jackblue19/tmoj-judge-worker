namespace Application.UseCases.StudyPlans.Dtos;

public class StudyPlanStatsDto
{
    public Guid StudyPlanId { get; set; }

    public int TotalItems { get; set; }
    public int TotalUsersEnrolled { get; set; }

    public int TotalCompletedUsers { get; set; }

    public double CompletionRate { get; set; }

    public double AverageProgress { get; set; }
}