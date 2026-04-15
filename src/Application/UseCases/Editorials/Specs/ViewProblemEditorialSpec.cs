using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs
{
    public class ViewProblemEditorialSpec : Specification<Editorial>
    {
        public ViewProblemEditorialSpec(
            Guid problemId,
            Guid? cursorId,
            DateTime? cursorCreatedAt,
            int pageSize)
        {
            Query
                .Where(x => x.ProblemId == problemId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(pageSize);
        }
    }
}