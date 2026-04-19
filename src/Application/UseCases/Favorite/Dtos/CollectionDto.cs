namespace Application.UseCases.Favorite.Dtos;

public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public bool IsVisibility { get; set; }
    public DateTime CreatedAt { get; set; }
}