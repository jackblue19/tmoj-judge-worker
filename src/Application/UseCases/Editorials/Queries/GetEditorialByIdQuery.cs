using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.Editorials.Queries
{
    public record GetEditorialByIdQuery(Guid EditorialId) : IRequest<EditorialDto>;
}
