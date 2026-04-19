public class ReorderCollectionRequest
{
    public List<ReorderItemDto> Items { get; set; } = new();
}

public class ReorderItemDto
{
    public Guid ItemId { get; set; }
    public int OrderIndex { get; set; }
}