using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ProblemEditorials.Specs
{
    public class ViewProblemEditorialSpec : Specification<ProblemEditorial>
    {
        public ViewProblemEditorialSpec(Guid problemId, int pageSize)
        {
            Query
                .Where(x => x.ProblemId == problemId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(pageSize);
        }
    }
}