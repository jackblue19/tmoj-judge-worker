namespace Application.UseCases.Gamification.Dtos;

public class DailyActivityDto
{
    public string Date { get; set; } = default!;
    public int Count { get; set; }
}
