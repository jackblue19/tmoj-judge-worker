using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContestById;

public class GetClassContestByIdQueryHandler : IRequestHandler<GetClassContestByIdQuery, ClassContestDto>
{
    private readonly IClassRepository _repo;

    public GetClassContestByIdQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<ClassContestDto> Handle(GetClassContestByIdQuery request, CancellationToken ct) =>
        _repo.GetClassContestByIdAsync(request.ClassSemesterId, request.ContestId, request.UserId, ct);
}
