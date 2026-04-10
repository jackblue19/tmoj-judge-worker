using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public class DeleteEditorialCommandHandler
        : IRequestHandler<DeleteEditorialCommand>
    {
        private readonly IEditorialRepository _repo;

        public DeleteEditorialCommandHandler(IEditorialRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteEditorialCommand request, CancellationToken cancellationToken)
        {
            var editorial = await _repo.GetByIdAsync(request.EditorialId);

            if (editorial == null)
                throw new Exception("Editorial not found");

            _repo.Delete(editorial);

            await _repo.SaveChangesAsync();
        }
    }
}