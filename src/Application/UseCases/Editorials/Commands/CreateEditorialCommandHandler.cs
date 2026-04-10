using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public class CreateEditorialCommandHandler
        : IRequestHandler<CreateEditorialCommand, Guid>
    {
        private readonly IEditorialRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public CreateEditorialCommandHandler(
            IEditorialRepository repo,
            ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateEditorialCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;

            var editorial = new Editorial
            {
                EditorialId = Guid.NewGuid(),
                ProblemId = request.ProblemId,
                AuthorId = userId,
                StorageId = request.StorageId,
                CreatedAt = DateTime.UtcNow
            };

            var id = await _repo.CreateAsync(editorial);

            return id;
        }
    }
}