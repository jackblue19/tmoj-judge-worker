using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.ProblemEditorials.Queries;

public class GetProblemEditorialsQueryHandler
    : IRequestHandler<GetProblemEditorialsQuery, List<ProblemEditorialDto>>
{
    private readonly IProblemEditorialRepository _repo;

    public GetProblemEditorialsQueryHandler(IProblemEditorialRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ProblemEditorialDto>> Handle(
        GetProblemEditorialsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repo.GetByProblemIdAsync(request.ProblemId, request.PageSize);

        return list.Select(x => new ProblemEditorialDto
        {
            Id = x.Id,
            ProblemId = x.ProblemId,
            AuthorId = x.AuthorId,
            Content = x.Content,

            // 🔥 FIX
            CreatedAt = x.CreatedAt ?? DateTime.Now,
            UpdatedAt = x.UpdatedAt ?? DateTime.Now

        }).ToList();
    }
}