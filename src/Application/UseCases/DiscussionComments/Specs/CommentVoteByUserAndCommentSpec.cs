using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs;

public class CommentVoteByUserAndCommentSpec : Specification<CommentVote>
{
    public CommentVoteByUserAndCommentSpec(Guid userId, Guid commentId)
    {
        Query.Where(v => v.UserId == userId && v.CommentId == commentId);
    }
}