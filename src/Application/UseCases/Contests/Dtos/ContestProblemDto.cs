namespace Application.UseCases.Contests.Dtos;

public class ContestProblemDto
{
    public Guid Id { get; set; }          // contest_problem_id
    public Guid ProblemId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Alias { get; set; }    // A, B, C...
    public int? Points { get; set; }
}