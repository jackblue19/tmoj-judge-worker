using Application.Common.Pagination;
using Application.UseCases.ProblemDiscussions.Dtos;
using Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IProblemDiscussionRepository
    {
        /// <summary>
        /// Lấy danh sách discussion theo problemId dạng cursor pagination
        /// </summary>
        /// <param name="problemId">Id của problem</param>
        /// <param name="cursorCreatedAt">Ngày tạo cuối cùng của page trước (nullable)</param>
        /// <param name="cursorId">Id cuối cùng của page trước (nullable)</param>
        /// <param name="pageSize">Số lượng item mỗi page</param>
        /// <returns>CursorPaginationDto chứa danh sách DiscussionResponseDto</returns>
        Task<Common.Pagination.CursorPaginationDto<DiscussionResponseDto>> GetPagedAsync(
            Guid problemId,
            DateTime? cursorCreatedAt,
            Guid? cursorId,
            int pageSize);
        Task<DiscussionResponseDto?> GetByIdAsync(Guid id);

        Task<DiscussionResponseDto?> GetByIdWithUserAsync(Guid id);
        //Task LockAsync(Guid discussionId);
        //Task UnlockAsync(Guid discussionId);

    }
}