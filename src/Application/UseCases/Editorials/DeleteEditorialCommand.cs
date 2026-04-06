using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Editorials;

public record DeleteEditorialCommand(Guid EditorialId) : IRequest<Unit>;
