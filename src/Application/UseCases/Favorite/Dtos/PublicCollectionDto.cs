namespace Application.UseCases.Favorite.Dtos;

public class PublicCollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    // ✅ ADD MISSING
    public string Type { get; set; } = default!;
    public bool IsVisibility { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = default!;

    public int TotalItems { get; set; }

    // ✅ OPTIONAL (nếu mày đã add trước đó thì giữ)
    public int ProblemCount { get; set; }
    public int ContestCount { get; set; }
    public int SolvedCount { get; set; }
    public double SolvedPercent { get; set; }

    public List<PreviewItemDto> PreviewItems { get; set; } = new();
}