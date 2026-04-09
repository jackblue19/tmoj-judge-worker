using Application.UseCases.Editorials.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs
{
    public class ViewEditorialSpec
        : Specification<Editorial, EditorialDto>
    {
        public ViewEditorialSpec(
            Guid problemId,
            Guid? cursorId,
            DateTime? cursorCreatedAt,
            int pageSize)
        {
            Query
                .Where(e => e.ProblemId == problemId)
                .Include(e => e.Storage); // 🔥 đảm bảo join

            if (cursorId.HasValue && cursorCreatedAt.HasValue)
            {
                Query.Where(e =>
                    e.CreatedAt < cursorCreatedAt.Value ||
                    (e.CreatedAt == cursorCreatedAt.Value && e.EditorialId < cursorId.Value));
            }

            Query
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.EditorialId)
                .Take(pageSize);

            Query.Select(e => new EditorialDto(
                e.EditorialId,
                e.ProblemId,
                e.AuthorId,

                // 🔥 FIX NULL
                e.Storage != null ? e.Storage.FilePath : "",
                e.Storage != null ? e.Storage.FileType : "",

                e.CreatedAt,
                e.UpdatedAt
            ));
        }
    }
}