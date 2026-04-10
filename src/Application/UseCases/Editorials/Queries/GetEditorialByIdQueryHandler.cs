using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.Editorials.Queries
{
    public class GetEditorialByIdQueryHandler
        : IRequestHandler<GetEditorialByIdQuery, EditorialDto>
    {
        private readonly IEditorialRepository _repo;

        public GetEditorialByIdQueryHandler(IEditorialRepository repo)
        {
            _repo = repo;
        }

        public async Task<EditorialDto> Handle(GetEditorialByIdQuery request, CancellationToken cancellationToken)
        {
            var editorial = await _repo.GetByIdAsync(request.EditorialId);

            if (editorial == null)
                throw new Exception("Editorial not found");

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