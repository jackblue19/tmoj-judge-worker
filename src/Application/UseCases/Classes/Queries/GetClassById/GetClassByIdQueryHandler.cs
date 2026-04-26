using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassById;

public class GetClassByIdQueryHandler : IRequestHandler<GetClassByIdQuery, ClassDto>
{
    private readonly IClassRepository _repo;

    public GetClassByIdQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<ClassDto> Handle(GetClassByIdQuery request, CancellationToken ct) =>
        _repo.GetClassByIdAsync(request.ClassId, ct);
}
