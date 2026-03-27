using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs;

public class EditorialByProblemSpec : Specification<Editorial>
{
    public EditorialByProblemSpec(Guid problemId)
    {
        Query.Where(e => e.ProblemId == problemId);
    }
}