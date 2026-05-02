namespace Application.UseCases.Contests.Dtos;

public abstract class GetContestLeaderboardResponse
{
    public Guid ContestId { get; set; }
    public string ContestName { get; set; } = string.Empty;
    public string ScoringMode { get; set; } = "ioi";
    public string Status { get; set; } = "upcoming";
    public bool Frozen { get; set; }
    public List<ContestProblemHeaderDto> Problems { get; set; } = new();
    public string LastUpdated { get; set; } = DateTime.UtcNow.ToString("O");
}

public class ACMScoreboardResponse : GetContestLeaderboardResponse
{
    public List<ACMScoreboardRowDto> Rows { get; set; } = new();
}

public class IOIScoreboardResponse : GetContestLeaderboardResponse
{
    public List<IOIScoreboardRowDto> Rows { get; set; } = new();
}

public class ContestProblemHeaderDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? BalloonColor { get; set; }
    public int SolvedCount { get; set; }
    public int TotalAttempts { get; set; }
}

public class ACMScoreboardRowDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? EquippedFrameUrl { get; set; }
    public string? Fullname { get; set; }
    public int TotalSolved { get; set; }
    public int TotalPenalty { get; set; }
    public List<ACMProblemAttemptDto> Problems { get; set; } = new();
}

public class IOIScoreboardRowDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? EquippedFrameUrl { get; set; }
    public string? Fullname { get; set; }
    public int TotalScore { get; set; }
    public List<IOIProblemAttemptDto> Problems { get; set; } = new();
}

public class ACMProblemAttemptDto
{
    public string ProblemId { get; set; } = string.Empty;
    public bool IsSolved { get; set; }
    public bool IsFirstBlood { get; set; }
    public int AttemptsCount { get; set; }
    public int? PenaltyTime { get; set; }
}

public class IOIProblemAttemptDto
{
    public string ProblemId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int AttemptsCount { get; set; }
}

public class ScoreboardRowDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? EquippedFrameUrl { get; set; }
    public string? Fullname { get; set; }
    public int TotalSolved { get; set; }
    public int TotalPenalty { get; set; }
    public int TotalScore { get; set; }
    public List<ProblemAttemptDto> Problems { get; set; } = new();
}

public class ProblemAttemptDto
{
    public string ProblemId { get; set; } = string.Empty;
    public bool IsSolved { get; set; }
    public bool IsFirstBlood { get; set; }
    public int AttemptsCount { get; set; }
    public int? PenaltyTime { get; set; }
    public int? PendingCount { get; set; }
}

public class TeamLeaderboardDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int Solved { get; set; }
    public int Penalty { get; set; }
    public int TotalScore { get; set; }
    public int Rank { get; set; }
    public List<ProblemLeaderboardDto> Problems { get; set; } = new();
}

public class ProblemLeaderboardDto
{
    public Guid ProblemId { get; set; }
    public bool IsSolved { get; set; }
    public int WrongAttempts { get; set; }
    public DateTime? FirstAcAt { get; set; }
    public int Penalty { get; set; }
    public int Score { get; set; }
    public int PassedCases { get; set; }
    public int TotalCases { get; set; }
    public List<SubmissionTimelineDto> Submissions { get; set; } = new();
}

public class SubmissionTimelineDto
{
    public Guid SubmissionId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsAccepted { get; set; }
}
