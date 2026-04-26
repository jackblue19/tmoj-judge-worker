using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Semesters.Specs;

public class SemesterByCodeSpec : Specification<Semester>
{
    public SemesterByCodeSpec(string code)
    {
        Query.Where(s => s.Code == code);
    }
}
