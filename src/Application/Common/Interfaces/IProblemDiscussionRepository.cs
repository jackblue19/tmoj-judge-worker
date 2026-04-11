using Application.Common.Pagination;
using Application.UseCases.DiscussionComments.Dtos;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IProblemDiscussionRepository
    {
        Task<CursorPaginationDto<DiscussionResponseDto>> GetPagedAsync(
            Guid problemId,
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize);

        // 🔥 DTO dùng cho API
        Task<DiscussionResponseDto?> GetByIdAsync(Guid id);

        Task<List<ProblemDiscussion>> GetDiscussionEntitiesByIdsAsync(List<Guid> ids);


        Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id);

        Task<DiscussionResponseDto?> GetDiscussionWithCommentsTreeAsync(Guid discussionId);

        Task<DiscussionResponseDto?> GetByIdWithVoteAndCommentsAsync(Guid discussionId, Guid? userId);

       
        Task<ProblemDiscussion?> GetEntityByIdAsync(Guid id);

        Task DeleteDiscussionWithCommentsAsync(Guid discussionId);

        Task LockAsync(Guid discussionId);
        Task UnlockAsync(Guid discussionId);
    }
}