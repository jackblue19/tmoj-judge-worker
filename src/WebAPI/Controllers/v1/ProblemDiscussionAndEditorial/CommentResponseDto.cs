namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = "";
        public int VoteCount { get; set; }

        public Guid? ParentId { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<CommentResponseDto> Replies { get; set; } = new();
    }
}
