using System;

namespace Application.UseCases.ProblemDiscussions.Dtos;

public class UserActivityDto
{
    public Guid Id { get; set; }
    public Guid DiscussionId { get; set; }
    public Guid ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "discussion" | "comment"
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
