using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemEditorials.Specs
{
    public class ProblemEditorialByIdSpec : Specification<ProblemEditorial>
    {
        public ProblemEditorialByIdSpec(Guid id)
        {
            Query.Where(x => x.Id == id);
        }
    }
}