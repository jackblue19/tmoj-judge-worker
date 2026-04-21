namespace Application.UseCases.Favorite.Dtos;

public class PublicCollectionsResult
{
    public List<PublicCollectionDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}