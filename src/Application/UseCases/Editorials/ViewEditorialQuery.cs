using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Editorials
{
    public record ViewEditorialQuery(
    Guid ProblemId,              
    Guid? CursorId,
    DateTime? CursorCreatedAt,
    int PageSize = 10
) : IRequest<IReadOnlyList<EditorialDto>>;
}
