namespace Application.UseCases.StudyProgress.Dtos;

public class StudyPlanProgressDto
{
    public Guid StudyPlanId { get; set; }
    public Guid UserId { get; set; }

    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }

    public double ProgressPercent { get; set; }

    public List<StudyPlanItemProgressDto> Items { get; set; } = new();
}

public class StudyPlanItemProgressDto
{
    public Guid StudyPlanItemId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}