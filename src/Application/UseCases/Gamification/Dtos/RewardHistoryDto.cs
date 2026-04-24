namespace Application.UseCases.Gamification.Dtos;

public class RewardHistoryDto
{
    public string Type { get; set; } = default!; // solved | badge | streak | contest
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public string? Icon { get; set; }
    public decimal? CoinAmount { get; set; }
    public int? ExpAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}