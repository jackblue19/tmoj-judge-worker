namespace Application.UseCases.StudyPlans.Dtos;

public class StudyPlanEnrollmentDto
{
    public Guid StudyPlanId { get; set; }
    public Guid UserId { get; set; }

    public bool IsEnrolled { get; set; }
    public bool IsPurchased { get; set; }
    public bool IsCompleted { get; set; }

    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }

    public double ProgressPercent { get; set; }
}