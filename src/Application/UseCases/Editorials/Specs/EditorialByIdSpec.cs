using Application.UseCases.Editorials.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs
{
    public class EditorialByIdSpec : Specification<Editorial, EditorialDto>
    {
        public EditorialByIdSpec(Guid editorialId)
        {
            Query
                .Where(e => e.EditorialId == editorialId)
                .Include(e => e.Storage);

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