using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetMyClasses;

public class GetMyClassesQueryHandler : IRequestHandler<GetMyClassesQuery, ClassListDto>
{
    private readonly IClassRepository _repo;

    public GetMyClassesQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<ClassListDto> Handle(GetMyClassesQuery request, CancellationToken ct) =>
        _repo.GetMyClassesAsync(request.UserId, request.Role, request.SemesterId, request.SubjectId, request.PageNumber, request.PageSize, ct);
}
