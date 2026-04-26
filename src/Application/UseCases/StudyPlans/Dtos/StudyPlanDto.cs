namespace Application.UseCases.StudyPlans.Dtos;

public class StudyPlanDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";

    public int Order { get; set; }
    public int ProblemCount { get; set; }

    public int? Price { get; set; }

    public bool IsCompleted { get; set; }
    public bool IsUnlocked { get; set; }
}