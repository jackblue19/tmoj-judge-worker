namespace Application.UseCases.StudyProgress.Dtos;

public class MyStudyProgressDto
{
    public List<MyStudyPlanProgressItemDto> Items { get; set; } = new();
}