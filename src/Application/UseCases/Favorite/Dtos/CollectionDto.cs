namespace Application.UseCases.Favorite.Dtos;

public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public bool IsVisibility { get; set; }
    public int TotalItems { get; set; }
    public int ProblemCount { get; set; }
    public int ContestCount { get; set; }
    public int SolvedCount { get; set; }
    public double SolvedPercent { get; set; }
    public DateTime CreatedAt { get; set; }
}