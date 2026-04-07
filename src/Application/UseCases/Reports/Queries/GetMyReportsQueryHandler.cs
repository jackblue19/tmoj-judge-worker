using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Queries;

public class GetMyReportsQueryHandler
    : IRequestHandler<GetMyReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyReportsQueryHandler(
        IReadRepository<ContentReport, Guid> repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<ReportDto>> Handle(GetMyReportsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var reports = await _repo.ListAsync(
            new ReportsByUserSpec(userId), ct);

        return reports.Select(x => new ReportDto
        {
            Id = x.Id,
            TargetId = x.TargetId,
            TargetType = x.TargetType,
            Reason = x.Reason,
            Status = x.Status,
            CreatedAt = x.CreatedAt
        }).ToList();
    }
}