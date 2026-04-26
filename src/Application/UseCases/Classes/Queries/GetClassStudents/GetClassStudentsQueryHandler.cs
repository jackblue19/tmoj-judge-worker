using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassStudents;

public class GetClassStudentsQueryHandler : IRequestHandler<GetClassStudentsQuery, List<ClassMemberDto>>
{
    private readonly IClassRepository _repo;

    public GetClassStudentsQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<List<ClassMemberDto>> Handle(GetClassStudentsQuery request, CancellationToken ct) =>
        _repo.GetClassStudentsAsync(request.ClassSemesterId, ct);
}
