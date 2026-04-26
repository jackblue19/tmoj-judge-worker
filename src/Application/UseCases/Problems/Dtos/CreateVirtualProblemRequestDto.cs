namespace Application.UseCases.Problems.Dtos;

public sealed class CreateVirtualProblemRequestDto
{
    public Guid? OriginProblemId { get; set; }
    public string? OriginProblemSlug { get; set; }

    public string? Slug { get; set; }
    public string? Title { get; set; }

    public string? VisibilityCode { get; set; }
}