using MediatR;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Commands
{
    public class UpdateEditorialCommand : IRequest<EditorialDto>
    {
        public Guid EditorialId { get; set; }
        public Guid StorageId { get; set; }
    }
}