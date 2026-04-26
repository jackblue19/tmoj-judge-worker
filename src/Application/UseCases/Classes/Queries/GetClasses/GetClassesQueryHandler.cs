using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClasses;

public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, ClassListDto>
{
    private readonly IClassRepository _repo;

    public GetClassesQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<ClassListDto> Handle(GetClassesQuery request, CancellationToken ct) =>
        _repo.GetClassesAsync(request.SemesterId, request.SubjectId, request.Search, request.PageNumber, request.PageSize, ct);
}
