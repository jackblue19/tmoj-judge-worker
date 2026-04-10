using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public class UpdateEditorialCommandHandler
        : IRequestHandler<UpdateEditorialCommand, EditorialDto>
    {
        private readonly IEditorialRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public UpdateEditorialCommandHandler(
            IEditorialRepository repo,
            ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<EditorialDto> Handle(UpdateEditorialCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var editorial = await _repo.GetByIdAsync(request.EditorialId);

            if (editorial == null)
                throw new Exception("Editorial not found");

            if (editorial.AuthorId == null || editorial.AuthorId != userId)
                throw new UnauthorizedAccessException();

            editorial.StorageId = request.StorageId;
            editorial.UpdatedAt = DateTime.UtcNow;

            _repo.Update(editorial);
            await _repo.SaveChangesAsync();

            return new EditorialDto
            {
                EditorialId = editorial.EditorialId,
                ProblemId = editorial.ProblemId,
                AuthorId = editorial.AuthorId,
                StorageId = editorial.StorageId,
                CreatedAt = editorial.CreatedAt,
                UpdatedAt = editorial.UpdatedAt ?? DateTime.UtcNow
            };
        }
    }
}