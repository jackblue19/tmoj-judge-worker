namespace Application.UseCases.ProblemDiscussions.Dtos
{
    public class DiscussionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProblemId { get; set; }
        public Guid UserId { get; set; }
        public string? UserDisplayName { get; set; }
        public string? UserAvatarUrl { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
