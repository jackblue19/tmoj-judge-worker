using MediatR;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Queries
{
    public class GetEditorialByIdQuery : IRequest<EditorialDto>
    {
        public Guid EditorialId { get; set; }
    }
}