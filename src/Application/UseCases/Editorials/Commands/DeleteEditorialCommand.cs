using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public class DeleteEditorialCommand : IRequest
    {
        public Guid EditorialId { get; set; }
    }
}