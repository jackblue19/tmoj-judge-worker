using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetAvailableStudents;

public class GetAvailableStudentsQueryHandler : IRequestHandler<GetAvailableStudentsQuery, PagedAvailableStudentsDto>
{
    private readonly IClassRepository _repo;

    public GetAvailableStudentsQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<PagedAvailableStudentsDto> Handle(GetAvailableStudentsQuery request, CancellationToken ct) =>
        _repo.GetAvailableStudentsAsync(request.ClassSemesterId, request.Search, request.PageNumber, request.PageSize, ct);
}
