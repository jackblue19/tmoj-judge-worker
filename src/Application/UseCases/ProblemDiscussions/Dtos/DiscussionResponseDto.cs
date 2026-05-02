namespace Application.UseCases.ProblemDiscussions.Dtos
{
    public class DiscussionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProblemId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public int VoteCount { get; set; }
        public int UserVote { get; set; }
        public bool IsHidden { get; set; }

        // Tree comment
        public List<DiscussionCommentResponseDto> Comments { get; set; } = new();
    }

  
}