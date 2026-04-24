using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Semesters.Specs;

public class SemesterOverlapSpec : Specification<Semester>
{
    public SemesterOverlapSpec(DateOnly startAt, DateOnly endAt, bool activeOnly = true)
    {
        if (activeOnly)
            Query.Where(s => s.IsActive);

        Query.Where(s => s.StartAt <= endAt && s.EndAt >= startAt);
    }
}

public class SemesterOverlapExceptIdSpec : Specification<Semester>
{
    public SemesterOverlapExceptIdSpec(Guid semesterId, DateOnly startAt, DateOnly endAt)
    {
        Query
            .Where(s => s.IsActive)
            .Where(s => s.SemesterId != semesterId)
            .Where(s => s.StartAt <= endAt && s.EndAt >= startAt);
    }
}
