namespace Application.UseCases.StudyProgress.Dtos;

public class MyStudyPlanProgressItemDto
{
    public Guid StudyPlanId { get; set; }
    public string Title { get; set; } = "";

    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }

    public double ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
}