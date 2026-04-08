using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.DiscussionComments.Specs;

public class CommentByIdSpec : Specification<DiscussionComment>
{
    public CommentByIdSpec(Guid commentId)
    {
        Query.Where(x => x.Id == commentId);
    }
}