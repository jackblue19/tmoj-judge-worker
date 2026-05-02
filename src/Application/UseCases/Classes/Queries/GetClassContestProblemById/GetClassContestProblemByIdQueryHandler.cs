using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContestProblemById;

public class GetClassContestProblemByIdQueryHandler : IRequestHandler<GetClassContestProblemByIdQuery, ContestProblemDto>
{
    private readonly IClassRepository _repo;

    public GetClassContestProblemByIdQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<ContestProblemDto> Handle(GetClassContestProblemByIdQuery request, CancellationToken ct) =>
        _repo.GetContestProblemByIdAsync(request.ClassSemesterId, request.ContestId, request.ContestProblemId, ct);
}
