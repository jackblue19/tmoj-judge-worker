using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using Application.UseCases.ProblemEditorials.Queries;
using MediatR;

namespace Application.UseCases.Editorials.Queries;

public class GetProblemEditorialByIdQueryHandler
    : IRequestHandler<GetProblemEditorialByIdQuery, ProblemEditorialDto>
{
    private readonly IProblemEditorialRepository _repo;

    public GetProblemEditorialByIdQueryHandler(IProblemEditorialRepository repo)
    {
        _repo = repo;
    }

    public async Task<ProblemEditorialDto> Handle(
        GetProblemEditorialByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id);

        if (entity == null)
            throw new Exception("Editorial not found");

        return new ProblemEditorialDto
        {
            Id = entity.Id,
            ProblemId = entity.ProblemId,
            AuthorId = entity.AuthorId,
            Content = entity.Content,

            // 🔥 FIX
            CreatedAt = entity.CreatedAt ?? DateTime.Now,
            UpdatedAt = entity.UpdatedAt ?? DateTime.Now
        };
    }
}