namespace Application.UseCases.Contests.Dtos;

public class ContestScoreboardDto
{
    public int Rank { get; set; }

    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;

    /// <summary>"acm" hoặc "ioi" — quyết định bởi Contest.ContestType.</summary>
    public string ScoringMode { get; set; } = "ioi";

    // ACM (solved count + ICPC penalty minutes)
    public int Solved { get; set; }
    public int Penalty { get; set; }

    // IOI (tổng điểm weight của best submission từng problem)
    public int TotalScore { get; set; }

    public List<ScoreboardProblemDto> Problems { get; set; } = new();
}

public class ScoreboardProblemDto
{
    public Guid ProblemId { get; set; }
    public string Alias { get; set; } = string.Empty;

    public bool IsSolved { get; set; }
    public int Attempts { get; set; }

    public DateTime? SolvedAt { get; set; }

    // IOI-specific: điểm weight best submission.
    public int Score { get; set; }
    public int PassedCases { get; set; }
    public int TotalCases { get; set; }
}