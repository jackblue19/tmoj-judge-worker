using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Editorials
{
    public record CreateEditorialCommand(
    Guid ProblemId,
    Guid StorageId
) : IRequest<Guid>;
}
