namespace Application.UseCases.Favorite.Dtos;

public class AddProblemToCollectionResult
{
    public bool IsSuccess { get; set; }
    public bool IsAlreadyExists { get; set; }

    public Guid? CollectionId { get; set; }
    public Guid? ProblemId { get; set; }
    public Guid? ItemId { get; set; }

    public string Message { get; set; } = string.Empty;
}