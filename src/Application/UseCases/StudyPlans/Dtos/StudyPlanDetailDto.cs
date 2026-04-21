namespace Application.UseCases.StudyPlans.Dtos;

public class StudyPlanDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    public List<StudyPlanItemDto> Items { get; set; } = new();
}

public class StudyPlanItemDto
{
    public Guid StudyPlanItemId { get; set; } // ✅ ADD THIS

    public Guid ProblemId { get; set; }

    public int Order { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsUnlocked { get; set; }
}