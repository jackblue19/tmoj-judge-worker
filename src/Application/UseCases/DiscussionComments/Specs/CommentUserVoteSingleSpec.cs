using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs;

public class CommentUserVoteSingleSpec : Specification<ContentVote>
{
    public CommentUserVoteSingleSpec(Guid userId, Guid commentId)
    {
        Query.Where(v => v.UserId == userId && v.TargetId == commentId && v.TargetType == "comment");
    }
}