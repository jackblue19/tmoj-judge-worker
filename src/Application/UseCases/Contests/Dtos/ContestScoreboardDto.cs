namespace Application.UseCases.Contests.Dtos;

public class ContestScoreboardDto
{
    public int Rank { get; set; }

    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;

    public int Solved { get; set; }
    public int Penalty { get; set; } // ICPC penalty (minutes)

    public List<ScoreboardProblemDto> Problems { get; set; } = new();
}

public class ScoreboardProblemDto
{
    public Guid ProblemId { get; set; }
    public string Alias { get; set; } = string.Empty;

    public bool IsSolved { get; set; }
    public int Attempts { get; set; }

    public DateTime? SolvedAt { get; set; }
}