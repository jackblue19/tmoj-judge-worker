namespace Application.UseCases.DiscussionComments.Dtos
{
    public class CommentCreateDto
    {
        public string Content { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }

    public class CommentUpdateDto
    {
        public string Content { get; set; } = string.Empty;
    }
    public class VoteCommentDto
    {
        /// <summary>
        /// 1 = upvote, -1 = downvote, 0 = unvote
        /// </summary>
        public int VoteType { get; set; }
    }
    public class HideUnhideCommentDto
    {
        public bool Hide { get; set; } // true → hide, false → unhide
    }

    public class CommentResponseDto
    {
        public Guid CommentId { get; set; }
        public Guid DiscussionId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

       
        public int VoteCount { get; set; }
        public int TotalVotes { get; set; }
        public int? UserVote { get; set; }

        public List<CommentResponseDto> Replies { get; set; } = new();
    }
}