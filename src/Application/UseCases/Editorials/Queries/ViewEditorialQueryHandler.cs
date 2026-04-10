using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.Editorials.Queries
{
    public class ViewEditorialQueryHandler
        : IRequestHandler<ViewEditorialQuery, List<EditorialDto>>
    {
        private readonly IEditorialRepository _repo;

        public ViewEditorialQueryHandler(IEditorialRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<EditorialDto>> Handle(ViewEditorialQuery request, CancellationToken cancellationToken)
        {
            var list = await _repo.GetByProblemIdAsync(request.ProblemId);

            var result = list
                .OrderByDescending(x => x.CreatedAt)
                .Take(request.PageSize)
                .Select(x => new EditorialDto
                {
                    EditorialId = x.EditorialId,
                    ProblemId = x.ProblemId,
                    AuthorId = x.AuthorId,
                    StorageId = x.StorageId,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt ?? DateTime.UtcNow
                })
                .ToList(); // 🔥 QUAN TRỌNG

            return result;
        }
    }
}