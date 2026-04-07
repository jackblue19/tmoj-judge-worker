using Application.Common.Pagination;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.ProblemDiscussions.Dtos;
using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IProblemDiscussionRepository
    {
        Task<CursorPaginationDto<DiscussionResponseDto>> GetPagedAsync(
            Guid problemId, DateTime? cursorCreatedAt, Guid? cursorId, int pageSize);

        Task<DiscussionResponseDto?> GetByIdAsync(Guid id);

        Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id);

        Task<DiscussionResponseDto?> GetDiscussionWithCommentsTreeAsync(Guid discussionId);

        Task<DiscussionResponseDto?> GetByIdWithVoteAndCommentsAsync(Guid discussionId, Guid? userId);
        Task DeleteDiscussionWithCommentsAsync(Guid discussionId);

        Task LockAsync(Guid discussionId);
        Task UnlockAsync(Guid discussionId);
    }
}