using Application.UseCases.Editorials.Dtos;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs;

public class EditorialByIdSpec : Specification<Editorial, EditorialDto>
{
    public EditorialByIdSpec(Guid editorialId)
    {
        Query.Include(e => e.Storage);

        Query.Where(e => e.EditorialId == editorialId);

        Query.Select(e => new EditorialDto(
            e.EditorialId,
            e.ProblemId,
            e.AuthorId,
            e.Storage.FilePath,
            e.Storage.FileType,
            e.CreatedAt,
            e.UpdatedAt
        ));
    }
}
