namespace Application.UseCases.Editorials.Dtos;

public class ProblemEditorialDto
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}