using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public record UpdateEditorialCommand(
    Guid EditorialId,
    Guid StorageId
) : IRequest<Unit>;
}
