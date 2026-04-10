using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs
{
    public class EditorialByIdSpec : Specification<Editorial>
    {
        public EditorialByIdSpec(Guid id)
        {
            Query.Where(x => x.EditorialId == id);
        }
    }
}