using Ardalis.Specification;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.UseCases.Contests.Specs
{
    public class ContestByIdSpec : Specification<Contest>
    {
        public ContestByIdSpec(Guid contestId)
        {
            Query.Where(x => x.Id == contestId);
        }
    }
}
