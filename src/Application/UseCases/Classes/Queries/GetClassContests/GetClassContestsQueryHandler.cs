using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContests;

public class GetClassContestsQueryHandler : IRequestHandler<GetClassContestsQuery, List<ClassContestSummaryDto>>
{
    private readonly IClassRepository _repo;

    public GetClassContestsQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<List<ClassContestSummaryDto>> Handle(GetClassContestsQuery request, CancellationToken ct) =>
        _repo.GetClassContestsAsync(request.ClassSemesterId, ct);
}
