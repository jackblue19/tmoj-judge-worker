
using Application.UseCases.DiscussionComments.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IDiscussionCommentRepository
    {
        Task<CommentResponseDto?> GetByIdWithUserAsync(Guid id);
        Task<List<CommentResponseDto>> GetByDiscussionIdAsync(Guid discussionId);

        Task<DiscussionComment?> GetByIdWithDiscussionAsync(Guid id);
    }
}