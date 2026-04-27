namespace Application.UseCases.Ranking.Dtos;

public class GlobalLeaderboardDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<GlobalLeaderboardRowDto> Rows { get; set; } = new();
}

public class GlobalLeaderboardRowDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? RollNumber { get; set; }
    public int Solved { get; set; }
    public int TotalAttempts { get; set; }
    public int Accuracy { get; set; }
    public int Points { get; set; }
}

public class PublicContestSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Rules { get; set; }
    public string Status { get; set; } = string.Empty;
}
