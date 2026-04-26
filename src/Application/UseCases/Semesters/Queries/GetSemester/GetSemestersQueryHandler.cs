using Application.UseCases.Semesters.Dtos;
using Application.UseCases.Semesters.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Semesters.Queries.GetSemester;

public class GetSemestersQueryHandler : IRequestHandler<GetSemestersQuery, SemesterListDto>
{
    private readonly IReadRepository<Semester, Guid> _readRepo;

    public GetSemestersQueryHandler(IReadRepository<Semester, Guid> readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<SemesterListDto> Handle(GetSemestersQuery request, CancellationToken ct)
    {
        var listSpec = new ActiveSemestersSpec(request.Search, request.Page, request.PageSize);
        var countSpec = new ActiveSemestersSearchSpec(request.Search);

        var semesters = await _readRepo.ListAsync(listSpec, ct);
        var totalCount = await _readRepo.CountAsync(countSpec, ct);

        var items = semesters.Select(x => new SemesterDto(
            x.SemesterId,
            x.Code,
            x.Name,
            x.StartAt,
            x.EndAt,
            x.IsActive,
            x.CreatedAt)).ToList();

        return new SemesterListDto(items, totalCount);
    }
}
