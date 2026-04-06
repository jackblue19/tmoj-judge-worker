
using Application.UseCases.DiscussionComments.Dtos;

namespace Application.Common.Interfaces
{
    public interface IDiscussionCommentRepository
    {
        Task<CommentResponseDto?> GetByIdWithUserAsync(Guid id);
        Task<List<CommentResponseDto>> GetByDiscussionIdAsync(Guid discussionId);
    }
}