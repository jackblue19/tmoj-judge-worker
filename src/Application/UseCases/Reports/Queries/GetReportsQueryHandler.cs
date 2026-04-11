using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Queries;

public class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IContentReportRepository _contentRepo;

    public GetReportsQueryHandler(
        IReadRepository<ContentReport, Guid> repo,
        IContentReportRepository contentRepo)
    {
        _repo = repo;
        _contentRepo = contentRepo;
    }

    public async Task<List<ReportDto>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        // 🔥 lấy list report
        var reports = await _repo.ListAsync(
            new AllReportsSpec(request.Status), ct);

        var result = new List<ReportDto>();

        foreach (var r in reports)
        {
            // 🔥 lấy author từ repo (đúng kiến trúc)
            var (authorId, authorName) =
                await _contentRepo.GetAuthorInfoAsync(r.TargetId, r.TargetType);

            result.Add(new ReportDto
            {
                Id = r.Id,
                TargetId = r.TargetId,
                TargetType = r.TargetType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,

                // 🔥 NEW FIELD
                AuthorId = authorId,
                AuthorName = authorName
            });
        }

        return result;
    }
}