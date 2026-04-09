using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.UseCases.Editorials.Specs;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Queries;

public class GetEditorialByIdQueryHandler
    : IRequestHandler<GetEditorialByIdQuery, EditorialDto?>
{
    private readonly IReadRepository<Editorial, Guid> _repo;

    public GetEditorialByIdQueryHandler(IReadRepository<Editorial, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<EditorialDto?> Handle(GetEditorialByIdQuery request, CancellationToken ct)
    {
        var result = await _repo.FirstOrDefaultAsync(
            new EditorialByIdSpec(request.EditorialId), ct);

        return result;
    }
}
