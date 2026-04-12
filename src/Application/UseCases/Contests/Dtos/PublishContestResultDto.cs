namespace Application.UseCases.Contests.Dtos;

public class PublishContestResultDto
{
    public Guid ContestId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}