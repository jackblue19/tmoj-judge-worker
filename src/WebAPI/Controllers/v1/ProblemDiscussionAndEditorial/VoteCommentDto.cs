namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    public class VoteCommentDto
    {
        public Guid CommentId { get; set; }

        // 1 = upvote
        // -1 = downvote
        // 0 = remove vote
        public short Vote { get; set; }
    }
}
