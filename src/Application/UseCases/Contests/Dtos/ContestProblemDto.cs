namespace Application.UseCases.Contests.Dtos;

public class ContestProblemDto
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }

    public string Alias { get; set; } = default!;
    public int? Ordinal { get; set; }
    public int? DisplayIndex { get; set; }

    public int Points { get; set; }
    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }

    public string Title { get; set; } = default!;
}