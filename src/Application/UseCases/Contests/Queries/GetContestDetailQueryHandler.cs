using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestDetailQueryHandler
    : IRequestHandler<GetContestDetailQuery, ContestDetailDto>
{
    private readonly IContestRepository _repo;

    public GetContestDetailQueryHandler(IContestRepository repo)
    {
        _repo = repo;
    }

    public async Task<ContestDetailDto> Handle(
        GetContestDetailQuery request,
        CancellationToken ct)
    {
        var result = await _repo.GetContestDetailAsync(request.ContestId);

        if (result == null)
            throw new Exception("Contest not found");

        return result;
    }
}