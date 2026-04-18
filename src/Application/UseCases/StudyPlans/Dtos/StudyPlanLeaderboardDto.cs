namespace Application.UseCases.StudyPlans.Dtos;

public class StudyPlanLeaderboardDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }

    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }

    public double ProgressPercent { get; set; }

    public DateTime? LastActivityAt { get; set; }
}